# ONE BRAIN Chrome Lab Extension

Manifest V3 client for the local ONE BRAIN Chrome Lab Bridge.

## Install

1. Open `chrome://extensions`.
2. Enable Developer mode.
3. Click `Load unpacked`.
4. Select `browser-extension/onebrain-chrome-lab`.

## Connect to bridge

1. Run the bridge on PC A:

```powershell
dotnet run --project src/OneBrain.ChromeLab.Bridge -- --host 0.0.0.0 --port 8787
```

2. Open the extension side panel on PC B.
3. Enter the IP of PC A and port `8787`.
4. Click `Test Health`, then `Connect`.

If Windows Firewall blocks the connection, allow inbound TCP `8787` on the private LAN profile. Do not expose this port to the internet.

## Safety

The extension never stores or receives provider secrets. Those belong only to the local bridge process.

The content script:

* redacts password values;
* blocks writes to password, token and OTP-like fields;
* flags captcha, 2FA and login pages;
* does not run remote scripts;
* does not execute arbitrary generated JavaScript.

Use the red STOP button to stop a run locally and notify the bridge.
