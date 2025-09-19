# Edgegap Unity Gen2 SDK

This plugin has been tested, and supports Unity versions 2021.3.0f1+, including all LTS releases, and Unity 6.

This plugin is intended to simplify integration of Gen2 Matchmaking through pre-defined set of SDK methods and example client & server runtime handler scripts.

## Install With Git (recommended)

### Benefits

- Installing our plugin this way will ensure you get the freshest updates the moment they come out, see [the update guide](#update-the-plugin-in-unity).

### Caveats

- Requirement: functioning git client installed, for example [git-scm](https://git-scm.com/).

### Instructions

1. Open your Unity project,
2. Select toolbar option **Window** -> **Package Manager**,
3. Click the **+** icon and select **Add package from git URL...**,
4. Input the following URL `https://github.com/edgegap/edgegap-unity-gen2-sdk.git`,
5. Click **Add** and wait for the Unity Package Manager to complete the installation.

## Install via ZIP archive

### Benefits

- Slightly easier as no git client is required.

### Caveats

- Installing our plugin this way will require you to manually replace plugin contents if you [wish to update it](#update-the-plugin-in-unity),
- The newtonsoft package (dependency) version required may not be compatible with your project if you're already using an older version of this package.

### Instructions

1. Select toolbar option **Window** -> **Package Manager**,
2. Click the **+** icon and select **Add package by name...**,
3. Input the name `com.unity.nuget.newtonsoft-json` and wait for the Unity Package Manager to complete the installation.,
4. Back to this github project - make sure you're on the `main` branch,
5. Click **<> Code**, then **Download ZIP**,
6. Paste the contents of the unzipped archive in your `Assets` folder within Unity project root.

## Other Sources

This is the only official distribution channel for this SDK, do not trust unverified sources!

## Plugin Usage

[Follow our Getting Started guide first, then explore our Matchmaking Scenarios for inspiration.](https://docs.edgegap.com/learn/matchmaking/getting-started-with-gen2)

### Import Simple Example

1. Find this package in Unity Package Manager window.
2. Open the `Samples` tab.
3. Click on **Import** next to **Gen2 Simple Example**.
4. Locate sample files in your project `Assets/Samples/Edgegap Gen2 SDK/{version}/Simple Example`.
5. Create an Empty GameObject in your scene and attach `Gen2ClientHandlerExample.cs` script.
6. Configure Gen2 `BaseUrl` and `AuthToken` values from dashboard.

### Usage Requirements

To take full advantage of our Unity Gen2 service, you will need to [Create an Edgegap Free Tier account](https://app.edgegap.com/auth/register). Our Free Tier let's you test and explore all of Gen2 features for free, no credit card required!

### Troubleshooting

> Unity Editor shows `[Package Manager Window] Error adding package: https://github.com/edgegap/edgegap-unity-gen2-sdk.git`

- If you’re adding our plugin via git URL, you will need to have a git client installed.

> Unity Editor 2021 shows `failed to resolve assembly: 'Edgegap.Gen2.SDK...`

- This is a known issue when using plugin with [Unity's Burst compiler](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.burst.html).
- Install plugin [via ZIP archive](#install-via-zip-archive) and delete `EdgegapGen2SDK.asmdef` in the plugin folder to resolve this.

> Visual Studio shows `type or namespace name could not be found` for Edgegap namespace.

1. In your Unity Editor, navigate to **Edit / Preferences / External Tools / Generate .csproj files**.
2. Make sure you have enabled **Git packages**.
3. Click **Regenerate project files**.

## Update the Plugin in Unity

Before updating, take note of your `Client Version` property on `Gen2Client.cs` to ensure future compatibility.

Depending on your installation method:

- If you installed with git, locate it in Unity's **Package Manager** window and click **Update**. Wait for the process to complete and you're good to go!
- If you installed via ZIP archive, you will need to remove the previous copy, then download the new version.

### Migrating Scenes

1. **Replace any missing scripts in your scenes!**

   - Verify validity of your `Gen2Client.cs` properties like `BaseUrl` and `AuthToken`.

2. **Increase `Client Version` property value of your `Gen2Client.cs` script!**

   - This will prevent loading outdated tickets from cache when matchmaking resumed.

3. You may want to modify your client & server handler scripts.

## For Plugin Developers

This section is only for developers working on this plugin or other plugins interacting / integrating this plugin.

### CSharpier Code Frmatter

This project uses [CSharpier code formatter](https://csharpier.com/) to ensure consistent and readable formatting, configured in `/.config/dotnet-tools.json`.

See [Editor integration](https://csharpier.com/docs/Editors) for Visual Studio extensions, optionally configure `Reformat with CSharpier` on Save under Tools | Options | CSharpier | General. You may also configure [running formatting as a pre-commit git hook](https://csharpier.com/docs/Pre-commit).
