"""Configuration initialization never replaces database or secret settings."""
import importlib.util
import json
import os
import stat
import tempfile
import unittest
from pathlib import Path


class InitializationTests(unittest.TestCase):
    def setUp(self):
        spec = importlib.util.spec_from_file_location('initialize_settings', Path(__file__).with_name('initialize-settings.py'))
        self.module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(self.module)

    def test_defaults_use_existing_public_address_and_keep_settings(self):
        source = {'Server': {'Address': 'https://ami.example.net/sub/'},
                  'Database': {'Type': 'PostgreSQL', 'Settings': 'existing secret'},
                  'Encryption': {'Key': 'keep'}, 'AllowedHosts': 'ami.example.net',
                  'Updater': {'Other': 123}}
        ami, updater = self.module.prepare(source, {})
        self.assertEqual(source['Database'], ami['Database'])
        self.assertEqual(source['Encryption'], ami['Encryption'])
        self.assertEqual(123, ami['Updater']['Other'])
        self.assertEqual('https://ami.example.net/sub/', updater['publicUrl'])
        self.assertEqual('https://ami.example.net/sub/api/Configuration/Version', updater['apiVersionUrl'])
        self.assertEqual('/srv/gurux-ami', ami['Updater']['Deployment']['Root'])
        self.assertTrue(updater['includeBackend'])
        self.assertNotIn('Deployment', source['Updater'])

    def test_existing_updater_values_are_retained_and_shared_with_ami(self):
        ami, updater = self.module.prepare({'Server': {'Address': 'https://ami.example.net'}},
                                          {'root': '/srv/custom', 'serviceName': 'custom.service',
                                           'catalogUrl': 'https://catalog.example.net/catalog.json',
                                           'checkTimeoutSeconds': 240, 'gitHubTokenEnvironmentVariable': 'PRIVATE_TOKEN'})
        self.assertEqual('/srv/custom', ami['Updater']['Deployment']['Root'])
        self.assertEqual('custom.service', updater['serviceName'])
        self.assertEqual(240, updater['checkTimeoutSeconds'])
        self.assertEqual('PRIVATE_TOKEN', updater['gitHubTokenEnvironmentVariable'])

    def test_missing_address_requires_installation_argument_before_writes(self):
        with self.assertRaisesRegex(ValueError, 'public-url'):
            self.module.prepare({}, {})

    def test_public_address_must_be_https_or_loopback_without_credentials(self):
        for address in ['http://ami.example.net', 'https://user:password@ami.example.net',
                        'https://ami.example.net/?token=secret', 'https://ami.example.net/#fragment',
                        'https://ami.example.com', 'https://ami.example.net:invalid',
                        'https://ami.example.net/#', 'https://ami.example.net/?', 'https://invalid host/']:
            with self.subTest(address=address), self.assertRaises(ValueError):
                self.module.prepare({'Server': {'Address': address}}, {})

    def test_explicit_installation_arguments_need_no_json_editing(self):
        ami, updater = self.module.prepare({}, {}, public_url='https://ami.example.net',
                                          root='/srv/ami', service='ami.service')
        self.assertEqual('https://ami.example.net/', updater['publicUrl'])
        self.assertEqual('/srv/ami', ami['Updater']['Deployment']['Root'])
        self.assertEqual('ami.service', updater['serviceName'])

    def test_repeated_initialization_is_idempotent_and_protects_file_mode(self):
        with tempfile.TemporaryDirectory() as directory:
            ami = Path(directory) / 'ami.json'
            updater = Path(directory) / 'updater.json'
            ami.write_text(json.dumps({'Server': {'Address': 'https://ami.example.net'},
                                       'Database': {'Settings': 'keep'}}))
            initial_mode = stat.S_IMODE(ami.stat().st_mode)
            self.module.initialize(ami, updater)
            first = (ami.read_bytes(), updater.read_bytes(), ami.stat().st_mtime_ns, updater.stat().st_mtime_ns)
            self.module.initialize(ami, updater)
            self.assertEqual(first, (ami.read_bytes(), updater.read_bytes(), ami.stat().st_mtime_ns, updater.stat().st_mtime_ns))
            self.assertEqual('keep', json.loads(ami.read_text())['Database']['Settings'])
            self.assertEqual(initial_mode, stat.S_IMODE(ami.stat().st_mode))
            if os.name == 'posix':
                self.assertEqual(0o640, stat.S_IMODE(updater.stat().st_mode))

    def test_invalid_configuration_does_not_create_or_modify_files(self):
        with tempfile.TemporaryDirectory() as directory:
            ami = Path(directory) / 'ami.json'
            updater = Path(directory) / 'updater.json'
            ami.write_text(json.dumps({'Database': {'Settings': 'keep'}}))
            original = ami.read_bytes()
            with self.assertRaises(ValueError):
                self.module.initialize(ami, updater)
            self.assertEqual(original, ami.read_bytes())
            self.assertFalse(updater.exists())

    def test_compose_configuration_is_not_overwritten_as_static(self):
        with self.assertRaisesRegex(ValueError, 'systemd'):
            self.module.prepare({'Server': {'Address': 'https://ami.example.net'}}, {'mode': 'compose'})


if __name__ == '__main__':
    unittest.main()
