# FritzboxPhonebookConv

A simple **.NET Framework 4.8 WPF** application for Windows that lets you:

1. **Connect** to a Fritz.Box router via **TR-064** (SOAP/HTTP Digest authentication).
2. **Browse** the list of phonebooks configured on the router and select one.
3. **Download** the selected phonebook as XML.
4. **Manage XSLT profiles** – named references to XSLT files on your local filesystem.
5. **Transform** the downloaded phonebook XML with the chosen XSLT stylesheet.
6. **Save** the transformed result to a file of your choice.

---

## Requirements

| Requirement | Version |
|-------------|---------|
| Windows OS  | 10 / 11 |
| .NET Framework | 4.8 |
| Fritz.Box firmware | Any version with TR-064 enabled (enabled by default) |

---

## Getting started

### Build

Open `FritzboxPhonebookConv.sln` in **Visual Studio 2019/2022** and press **Build → Build Solution**, or from a Developer Command Prompt:

```
dotnet build FritzboxPhonebookConv\FritzboxPhonebookConv.csproj
```

### Enable TR-064 on the Fritz.Box

1. Open the Fritz.Box admin page (e.g. `http://fritz.box`).
2. Go to **Home Network → Network → Network Settings**.
3. Enable **Allow access for applications** (TR-064).

### Run

Start `FritzboxPhonebookConv.exe` and follow the four-step workflow in the UI:

| Step | Action |
|------|--------|
| 1 | Enter the Fritz.Box host (`fritz.box` or its IP), port (`49000`), username, and password, then click **Connect**. |
| 2 | Choose a phonebook from the drop-down and click **Download XML**. |
| 3 | Add one or more XSLT profiles (a name + a `.xslt` / `.xsl` file path). |
| 4 | Choose the XSLT to apply, pick an output file, and click **▶ Transform & Save**. |

Settings (host, port, username, XSLT profiles, last output directory) are saved automatically to  
`%APPDATA%\FritzboxPhonebookConv\settings.xml`.  
The password is **not** saved for security reasons.

---

There are available xslt files which you might be able to use for your use-case here: https://github.com/blacksenator/fbcontactconv

## Sample XSLT

A ready-to-use stylesheet is provided under `FritzboxPhonebookConv/Samples/fritzbox_to_simple_xml.xslt`.  
It converts the Fritz.Box phonebook XML into a simplified `<contacts>` XML document with `<contact>` / `<phone>` elements.

### Fritz.Box source format (excerpt)

```xml
<phonebooks>
  <phonebook name="Telefonbuch">
    <contact>
      <person><realName>Max Mustermann</realName></person>
      <telephony>
        <number type="home" prio="1" id="0">030 12345678</number>
        <number type="mobile" prio="0" id="1">0171 987654</number>
      </telephony>
    </contact>
  </phonebook>
</phonebooks>
```

### Output produced by the sample XSLT

```xml
<contacts source="Telefonbuch">
  <contact>
    <name>Max Mustermann</name>
    <phone type="home">030 12345678</phone>
    <phone type="mobile">0171 987654</phone>
  </contact>
</contacts>
```

---

## Project structure

```
FritzboxPhonebookConv.sln
└── FritzboxPhonebookConv/
    ├── Models/
    │   ├── Phonebook.cs          – TR-064 phonebook entry (id, name, URL)
    │   └── XsltProfile.cs        – Named XSLT file reference
    ├── Services/
    │   ├── FritzBoxService.cs    – TR-064 SOAP client (GetPhonebookList, GetPhonebook, download)
    │   ├── XsltTransformService.cs – XSLT transformation via XslCompiledTransform
    │   └── SettingsService.cs    – XML-serialised application settings
    ├── ViewModels/
    │   ├── MainViewModel.cs      – MVVM view-model for MainWindow
    │   ├── RelayCommand.cs       – Synchronous ICommand
    │   └── AsyncRelayCommand.cs  – Async ICommand (Func<Task>)
    ├── Converters/
    │   └── InverseBoolConverter.cs
    ├── Samples/
    │   └── fritzbox_to_simple_xml.xslt
    ├── MainWindow.xaml / .xaml.cs
    └── App.xaml / .xaml.cs
```

---

## License

[MIT](LICENSE)
