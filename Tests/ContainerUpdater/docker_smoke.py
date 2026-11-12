"""Exercise only fresh, uniquely named Compose projects; never update an existing installation."""
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import tempfile
import time
import uuid

repo = Path(__file__).resolve().parents[2]

def run(arguments, cwd, check=True):
    result = subprocess.run(["docker", "compose", *arguments], cwd=cwd, text=True, capture_output=True)
    if check and result.returncode:
        raise RuntimeError(result.stderr)
    return result.stdout

def wait_healthy(project, service, seconds=45):
    deadline = time.monotonic() + seconds
    while time.monotonic() < deadline:
        container = run(["ps", "-q", service], project).strip()
        if container:
            value = json.loads(subprocess.check_output(["docker", "inspect", container], text=True))[0]
            if value["State"].get("Health", {}).get("Status") == "healthy":
                return container
        time.sleep(1)
    raise RuntimeError(service + " did not become healthy: " + run(["logs", "--tail", "15", service], project, False))

for target in ("ami", "agent"):
    directory = Path(tempfile.mkdtemp(prefix="gurux-compose-smoke-"))
    project_name = "gx-smoke-" + uuid.uuid4().hex[:12]
    try:
        compose = (repo / "deployment" / "docker" / target / "compose.yaml").read_text()
        compose = compose.replace("  " + target + ":\n    image:",
            "  " + target + ":\n    env_file: extra.env\n    secrets: [extra_secret]\n    configs: [extra_config]\n    image:", 1)
        compose += "\nsecrets:\n  extra_secret:\n    file: ./secret.txt\nconfigs:\n  extra_config:\n    file: ./config.txt\n"
        (directory / "compose.yaml").write_text(compose, encoding="utf-8", newline="\r\n")
        (directory / "extra.env").write_text("GX_EXTRA=present\r\n")
        (directory / "secret.txt").write_text("test-secret\r\n")
        (directory / "config.txt").write_text("test-config\r\n")
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", 0))
            port = sock.getsockname()[1]
        values = {"COMPOSE_PROJECT_NAME": project_name, "UPDATER_IMAGE": "gurux-ami-updater-test:local",
                  "AMI_IMAGE": "gurux-ami-server-test:local", "AMI_URL": "https://127.0.0.1:1",
                  "AMI_PORT": str(port), "AGENT_IMAGE": "gurux-ami-agent-test:local", "AGENT_TOKEN": "test-token",
                  "AGENT_ID": "11111111-2222-3333-4444-555555555555"}
        (directory / ".env").write_text("".join(key + "=" + value + "\n" for key, value in values.items()), encoding="utf-8", newline="\r\n")
        run(["up", "-d"], directory)
        updater = wait_healthy(directory, "updater")
        app = run(["ps", "-q", target], directory).strip()
        assert app, "The application was not started after automatic updater bootstrap"
        inspected = json.loads(subprocess.check_output(["docker", "inspect", app], text=True))[0]
        assert not any(m["Destination"] == "/var/run/docker.sock" for m in inspected["Mounts"])
        root = next(m for m in inspected["Mounts"] if m["Destination"] == "/run/gurux-updater")
        assert not root["RW"], "The application may not write updater status or key storage"
        subprocess.run(["docker", "exec", "-u", "1654", app, "test", "-r", "/run/gurux-updater/request.key"], check=True)
        subprocess.run(["docker", "exec", "-u", "1654", app, "test", "-w", "/run/gurux-updater/queue"], check=True)
        key = subprocess.check_output(["docker", "exec", updater, "cat", "/state/request.key"], text=True)
        assert len(key.strip()) >= 64
        assert json.loads((directory / "compose.override.yaml").read_text())["x-gurux-updater"] is True
        run(["restart", "updater"], directory)
        updater = wait_healthy(directory, "updater")
        assert subprocess.check_output(["docker", "exec", updater, "cat", "/state/request.key"], text=True) == key
        project_mount = next(m for m in json.loads(subprocess.check_output(["docker", "inspect", updater], text=True))[0]["Mounts"] if m["Destination"] == "/project")
        compose_command = ["docker", "exec", updater, "docker", "compose", "--env-file", "/project/.env", "--project-name", project_name,
                           "--project-directory", project_mount["Source"], "-f", "/project/compose.yaml", "-f", "/project/compose.override.yaml"]
        effective = json.loads(subprocess.check_output([*compose_command, "config", "--format", "json"], text=True))
        assert effective["services"][target]["environment"]["GX_EXTRA"] == "present"
        assert effective["secrets"]["extra_secret"]["file"] == project_mount["Source"] + "/secret.txt"
        assert effective["configs"]["extra_config"]["file"] == project_mount["Source"] + "/config.txt"
        subprocess.run([*compose_command, "up", "-d", "--no-deps", "--force-recreate", "--pull", "never", target], check=True, capture_output=True)
        recreated = run(["ps", "-q", target], directory).strip()
        assert recreated and recreated != app, "The updater must recreate the application using the installation's actual configuration"
        app = recreated
        if target == "agent":
            deadline = time.monotonic() + 15
            while time.monotonic() < deadline:
                result = subprocess.run(["docker", "exec", app, "test", "-f", "/data/settings.json"])
                if result.returncode == 0:
                    break
                time.sleep(1)
            assert result.returncode == 0, "Environment-based registration must persist without an interactive prompt"
        print("PASS automatic", target, "Compose startup, queue/key permissions, socket isolation idempotent updater restart and env_file/secret/config recreation", flush=True)
    except Exception:
        print(run(["logs", "--tail", "25", "updater"], directory, False), flush=True)
        raise
    finally:
        run(["down", "--volumes", "--remove-orphans"], directory, False)
        assert directory.resolve().parent == Path(tempfile.gettempdir()).resolve()
        shutil.rmtree(directory)

# A real process can remain drained and healthy while the helper pulls a new image.
# A different process must never inherit that heartbeat, even with the same image/version.
health_directory = Path(tempfile.mkdtemp(prefix="gurux-health-smoke-"))
reporter = "gx-health-" + uuid.uuid4().hex[:12]
replacement = "gx-health-" + uuid.uuid4().hex[:12]
try:
    published = health_directory / "publish"
    subprocess.run(["dotnet", "publish", str(repo / ".github" / "tests" / "AgentHealthReporter" / "AgentHealthReporter.csproj"),
                    "-c", "Release", "--artifacts-path", str(health_directory / "artifacts"), "-o", str(published),
                    "-p:GeneratePackageOnBuild=false", "-p:UseAppHost=false", "--verbosity", "minimal"], check=True)
    state = health_directory / "state"
    state.mkdir()
    subprocess.run(["docker", "run", "-d", "--name", reporter, "--user", str(os.getuid()), "--entrypoint", "dotnet",
                    "-v", str(published) + ":/tests:ro", "-v", str(state) + ":/state", "gurux-ami-agent-test:local",
                    "/tests/ContainerUpdater.Tests.dll", "--health-reporter", "/state"], check=True, capture_output=True)
    health_command = ["dotnet", "Gurux.DLMS.AMI.Agent.dll", "--health-check"]
    deadline = time.monotonic() + 20
    while time.monotonic() < deadline:
        healthy = subprocess.run(["docker", "exec", "-e", "Updater__DeploymentRoot=/state", reporter, *health_command], capture_output=True)
        if healthy.returncode == 0:
            break
        time.sleep(1)
    assert healthy.returncode == 0, "A fresh live/drained process must pass the production health check"
    subprocess.run(["docker", "stop", "--time", "5", reporter], check=True, capture_output=True)
    subprocess.run(["docker", "run", "-d", "--name", replacement, "--user", str(os.getuid()), "--entrypoint", "sleep",
                    "-v", str(state) + ":/state", "gurux-ami-agent-test:local", "60"], check=True, capture_output=True)
    old = subprocess.run(["docker", "exec", "-e", "Updater__DeploymentRoot=/state", replacement, *health_command], capture_output=True)
    assert old.returncode == 1, "A restarted image must reject the previous process's drained heartbeat"
    print("PASS live drained-process health and stale-process rejection across real container replacement", flush=True)
finally:
    subprocess.run(["docker", "rm", "-f", reporter, replacement], capture_output=True)
    assert health_directory.resolve().parent == Path(tempfile.gettempdir()).resolve()
    shutil.rmtree(health_directory)
