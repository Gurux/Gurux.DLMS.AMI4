# Linux systemd updates from the AMI user interface

AMI uses the separate **Gurux.Updater.Tool deployment-agent** to install a complete backend and browser release. The `/updates` page queues a catalog version; it does not run `systemctl` or replace its own files. The independent updater verifies the package, switches `current`, restarts AMI, checks the public application and restores the previous release when verification fails.

## First installation or migration

The initial deployment-enabled AMI build and the updater must be installed once by the Linux administrator. Version discovery alone does not make an older AMI installation capable of updating itself. The checked-in defaults use `gurux-ami.service` and `/srv/gurux-ami`. The initializer reads the real public URL from the existing AMI `Server.Address` setting. Installation arguments support other paths or unit names without hand-editing JSON.

1. Back up the database, current binaries, settings, encryption/data-protection keys, modules and uploaded files. Stop the existing AMI service for the initial migration. Preserve its existing settings and secrets; **do not replace them with the sample database settings**.
2. Install the ASP.NET Core 10 runtime. Obtain a Gurux.Updater.Tool build that supports `deployment-agent` and install its published files in `/opt/gurux-updater`, outside the AMI installation. For a source build, run `dotnet publish Development/Gurux.Updater.Tool/Gurux.Updater.Tool.csproj -c Release -o /opt/gurux-updater` from the Gurux.Updater.Net repository. The executable and its configuration must be administrator-owned.
3. Create the `gurux-ami` service account if it does not already exist. Create `/srv/gurux-ami/releases`, `/srv/gurux-ami/queue`, `/etc/gurux-ami` and `/var/lib/gurux-ami`. Put a complete published AMI package, including `wwwroot/version.json`, in `/srv/gurux-ami/releases/<version>`. Set the relative symlink `/srv/gurux-ami/current` to `releases/<version>`. Use a published release, not a build-output directory.
4. Keep persistent application data in `/var/lib/gurux-ami` and settings in `/etc/gurux-ami/ami-settings.json`. Move the existing data and keys with the service stopped, and update database paths and any absolute module/file references as required. Keep the existing AMI settings file; the initializer merges the updater section while preserving database settings, secrets and all other application configuration.
5. Initialize both JSON files from the checked-in [defaults](updater.defaults.json), using Python 3 from the AMI repository root:

```sh
sudo python3 deployment/systemd/initialize-settings.py
```

The default input is the existing `/etc/gurux-ami/ami-settings.json`; output is `/etc/gurux-ami/updater.json`. The command derives the public health-check and API-version URLs from `Server.Address`, sets full-backend static deployment, and aligns the catalog/root/product in both configurations. Existing updater options and file ownership/access modes are retained. Newly created updater settings use mode `0640`. Repeating the command with unchanged settings does not rewrite files. It does not install releases, restart services or change database settings.

If the public URL is supplied through the service environment rather than the JSON, pass it once as an installation argument:

```sh
sudo python3 deployment/systemd/initialize-settings.py --public-url https://YOUR_AMI_HOST/
```

For a different existing settings path, root or unit name, use `--ami-settings /path/to/ami-settings.json`, `--root /srv/your-ami`, or `--service your-ami.service`. Missing/invalid settings and placeholder addresses are rejected before writes. Do not run the older full-settings example over an existing installation. [updater.example.json](updater.example.json) remains a reference for the complete generated shape.

6. Give the backend read-only access to release directories and updater status. Only the independent updater may modify releases, `current`, history and status. Give the backend write access to its persistent data and the request queue. The example permissions below assume the account and directories already exist.

```sh
sudo chown root:gurux-ami /srv/gurux-ami /srv/gurux-ami/releases
sudo chmod 0755 /srv/gurux-ami /srv/gurux-ami/releases
sudo chown root:gurux-ami /srv/gurux-ami/queue
sudo chmod 2770 /srv/gurux-ami/queue
sudo touch /srv/gurux-ami/queue/queue.lock
sudo chown root:gurux-ami /srv/gurux-ami/queue/queue.lock
sudo chmod 0660 /srv/gurux-ami/queue/queue.lock
sudo chown root:gurux-ami /etc/gurux-ami/ami-settings.json /etc/gurux-ami/updater.json
sudo chmod 0640 /etc/gurux-ami/ami-settings.json /etc/gurux-ami/updater.json
```

7. Install [gurux-ami.service](gurux-ami.service) and [gurux-ami-updater.service](gurux-ami-updater.service) in `/etc/systemd/system`. Keep them independent: the updater must survive the backend being stopped. The sample updater runs as root to manage the configured backend service and protected release directories; the AMI backend runs as `gurux-ami`. Protect both units and `/opt/gurux-updater` against writes by the backend account.
8. Keep the existing HTTPS reverse proxy. Proxy dynamic `/api/`, `/Account/` and `/guruxami` requests to AMI. When serving the standalone browser files directly, serve `/srv/gurux-ami/current/wwwroot`, retain SPA routing, and disable caching of `index.html`, `version.json` and bootstrap resources. The updater checks the public URL, so TLS and reverse-proxy routing must work from the host itself.

Run the following from the AMI repository root after adapting the unit files:

```sh
sudo install -o root -g root -m 0644 deployment/systemd/gurux-ami.service /etc/systemd/system/gurux-ami.service
sudo install -o root -g root -m 0644 deployment/systemd/gurux-ami-updater.service /etc/systemd/system/gurux-ami-updater.service
sudo systemctl daemon-reload
sudo systemctl enable --now gurux-ami-updater.service
sudo systemctl enable --now gurux-ami.service
sudo systemctl status gurux-ami-updater.service gurux-ami.service
```

## Updating and restoring

Open `/updates` with the update-administrator permission. AMI reports whether it is running in systemd, a container or another environment. Detection is informational: an update still requires the configured independent updater and its recent heartbeat.

Select a newer catalog release and choose **Update AMI and user interface**. The browser submits only the version; the updater resolves the package independently and verifies its SHA-256. Expect a brief backend outage. Progress and the transaction ID survive the restart; reload the application when the update succeeds. **Restore previous version** is available for an eligible retained predecessor.

Before production use, test an upgrade and a failed-start rollback with the actual Linux service, public proxy and persistent data. Binary rollback does not undo database migrations. Back up the database before every upgrade and plan database restore separately for incompatible schema changes.

Inspect `journalctl -u gurux-ami-updater.service -u gurux-ami.service` and `/srv/gurux-ami/status.json` if the agent is unavailable or an update fails. Do not expose the queue, updater configuration, history or private credentials through the web server.
