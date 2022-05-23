# Package Starter Kit

The purpose of this starter kit is to provide the data structure and development guidelines for new packages meant for the **Unity Package Manager (UPM)**.

## Are you ready to become a package?
The Package Manager is a work in progress for Unity. Because of that, your package needs to meet these criteria to become an official Unity package:
- **Your code accesses public Unity C# APIs only.**
- **Your code doesn't require security, obfuscation, or conditional access control.**


## Package structure

```none
<root>
  ├── package.json
  ├── README.md
  ├── CHANGELOG.md
  ├── Third Party Notices.md
  ├── Editor
  │   ├── Undefined.AssemblyHotfixToolkit.Editor.asmdef
  │   └── EditorExample.cs
  ├── Runtime
  │   ├── Undefined.AssemblyHotfixToolkit.asmdef
  │   └── RuntimeExample.cs
  ├── Tests
  │   ├── .tests.json
  │   ├── Editor
  │   │   ├── Undefined.AssemblyHotfixToolkit.Editor.Tests.asmdef
  │   │   └── EditorExampleTest.cs
  │   └── Runtime
  │        ├── Undefined.AssemblyHotfixToolkit.Tests.asmdef
  │        └── RuntimeExampleTest.cs
  ├── Samples
  │   └── Example
  │       ├── .sample.json
  │       └── SampleExample.cs
  └── Documentation
       ├── Assembly Hotfix Toolkit.md
       └── Images
```

## Develop your package
Package development works best within the Unity Editor.  Here's how to get started:

1. Enter your package name. The name you choose should contain your default organization followed by the name you typed. For example: `Undefined.AssemblyHotfixToolkit`.

2. [Enter the information](#FillOutFields) for your package in the `package.json` file.

3. [Rename and update](#Asmdef) assembly definition files.

4. [Document](#Doc) your package.

5. [Add samples](#Populate) to your package (code & assets).

6. [Validate](#Valid) your package.

7. [Add tests](#Tests) to your package.

8. Update the `CHANGELOG.md` file. 

    Every new feature or bug fix should have a trace in this file. For more details on the chosen changelog format, see [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

9. Make sure your package [meets all legal requirements](#Legal).

10. Publish your package.



<a name="FillOutFields"></a>
### Completing the package manifest

You can either modify the package manifest (`package.json`) file directly in the Inspector or by using an external editor. 

To use the Inspector, select the `package.json` file in the Project browser. The **Package Assembly Hotfix Toolkit Manifest** page opens for editing.

Update these required attributes in the `package.json` file: 

| **Attribute name:** | **Description:**                                             |
| ------------------- | ------------------------------------------------------------ |
| **name**            | The officially registered package name. This name must conform to the [Unity Package Manager naming convention](https://docs.unity3d.com/Manual/upm-manifestPkg.html#name), which uses reverse domain name notation. For example: <br />`"com.[YourCompanyName].[your-package-name]"` |
| **displayName**     | A user-friendly name to appear in the Unity Editor (for example, in the Project Browser, the Package Manager window, etc.). For example: <br />`"Terrain Builder SDK"` <br/>__NOTE:__ Use a display name that will help users understand what your package is intended for. |
| **version**         | The package version number (**'MAJOR.MINOR.PATCH"**). This value must respect [semantic versioning](http://semver.org/). For more information, see [Package version](https://docs.unity3d.com/Manual/upm-manifestPkg.html#pkg-ver) in the Unity User Manual. |
| **unity**           | The lowest Unity version the package is compatible with. If omitted, the package is considered compatible with all Unity versions. <br /><br />The expected format is "**&lt;MAJOR&gt;.&lt;MINOR&gt;**" (for example, **2018.3**). |
| **description**     | A brief description of the package. This is the text that appears in the [details view](upm-ui-details) of the Packages window. Any [UTF-8](https://en.wikipedia.org/wiki/UTF-8) character code is supported. This means that you can use special formatting character codes, such as line breaks (**\n**) and bullets (**\u25AA**). |

Update the following recommended fields in file **package.json**:

| **Attribute name:** | **Description:**                                             |
| ------------------- | ------------------------------------------------------------ |
| **dependencies**    | A map of package dependencies. Keys are package names, and values are specific versions. They indicate other packages that this package depends on. For more information, see [Dependencies](https://docs.unity3d.com/Manual/upm-dependencies.html) in the Unity User Manual.<br /><br />**NOTE**: The Package Manager does not support range syntax, only **SemVer** versions. |
| **keywords**        | An array of keywords used by the Package Manager search APIs. This helps users find relevant packages. |



<a name="Asmdef"></a>
### Updating the Assembly Definition files

You must associate scripts inside a package to an assembly definition file (.asmdef). Assembly definition files are the Unity equivalent to a C# project in the .NET ecosystem. You must set explicit references in the assembly definition file to other assemblies (whether in the same package or in external packages). See [Assembly Definitions](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html) for more details.

Use these conventions for naming and storing your assembly definition files to ensure that the compiled assembly filenames follow the [.NET Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/):

* Store Editor-specific code under a root editor assembly definition file:

  `Editor/Undefined.AssemblyHotfixToolkit.Editor.asmdef`

* Store runtime-specific code under a root runtime assembly definition file:

  `Runtime/Undefined.AssemblyHotfixToolkit.asmdef`

* Configure related test assemblies for your editor and runtime scripts:

  `Tests/Editor/Undefined.AssemblyHotfixToolkit.Editor.Tests.asmdef`

  `Tests/Runtime/Undefined.AssemblyHotfixToolkit.Tests.asmdef`

To get a more general view of a recommended package folder layout, see [Package layout](https://docs.unity3d.com/Manual/cus-layout.html).



<a name="Doc"></a>
### Providing documentation

Use the `Documentations~/Assembly Hotfix Toolkit.md` documentation file to create preliminary, high-level documentation. This document should introduce users to the features and sample files included in your package.  Your package documentation files will be used to generate online and local docs, available from the Package Manager UI.

**Document your public APIs**
* All public APIs need to be documented with **XmlDoc**.
* API documentation is generated from [XmlDoc tags](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/xml-documentation-comments) included with all public APIs found in the package. See [Editor/EditorExample.cs](Editor/EditorExample.cs) for an example.




<a name="Populate"></a>
### Adding Assets to your package

If your package contains a sample, rename the `Samples/Example` folder, and update the `.sample.json` file in it.

In the case where your package contains multiple samples, you can make a copy of the `Samples/Example` folder for each sample, and update the `.sample.json` file accordingly.

Similar to `.tests.json` file, there is a `"createSeparatePackage"` field in `.sample.json`. If set to true, the CI will create a separate package for the sample.

Delete the `Samples` folder altogether if your package does not need samples.

As of Unity release 2019.1, the Package Manager recognizes the `/Samples` directory in a package. Unity doesn't automatically import samples when a user adds the package to a Project. However, users can click a button in the details view of a package in the **Packages** window to optionally import samples into their `/Assets` directory.




<a name="Valid"></a>
### Validating your package

Before you publish your package, you need to make sure that it passes all the necessary validation checks by using the Package Validation Suite extension (optional).

Once you install the Validation Suite package, a **Validate** button appears in the details view of a package in the **Packages** window. To install the extension, follow these steps:

1. Point your Project manifest to a staging registry by adding this line to the manifest: 
    `"registry": "https://staging-packages.unity.com"`
2. Install the **Package Validation Suite v0.3.0-preview.13** or above from the **Packages** window in Unity. Make sure the package scope is set to **All Packages**, and select **Show preview packages** from the **Advanced** menu.
3. After installation, a **Validate** button appears in the **Packages** window. Click the button to run a series of tests, then click the **See Results** button for additional information:
    * If it succeeds, a green bar with a **Success** message appears.
    * If it fails, a red bar with a **Failed** message appears.

**NOTE:** The validation suite is still in preview.




<a name="Tests"></a>
### Adding tests to your package

All packages must contain tests.  Tests are essential for Unity to ensure that the package works as expected in different scenarios.

**Editor tests**
* Write all your Editor Tests in `Tests/Editor`

**Playmode Tests**

* Write all your Playmode Tests in `Tests/Runtime`.

#### Separating the tests from the package

You can create a separate package for the tests, which allows you to exclude a large number of tests and Assets from being published in your main package, while still making it easy to test it.

Open the `Tests/.tests.json` file and set the **createSeparatePackage** attribute:

| **Value to set:** | **Result:**                                                  |
| ----------------- | ------------------------------------------------------------ |
| **true**          | CI creates a separate package for these tests. At publish time, the Package Manager adds metadata to link the packages together. |
| **false**         | Keep the tests as part of the published package.             |



<a name="Legal"></a>
### Meeting the legal requirements

You can use the Third Party Notices.md file to make sure your package meets any legal requirements. For example, here is a sample license file from the Unity Timeline package:

```
Unity Timeline copyright © 2017-2019 Unity Technologies ApS

Licensed under the Unity Companion License for Unity-dependent projects--see [Unity Companion License](http://www.unity3d.com/legal/licenses/Unity_Companion_License).

Unless expressly provided otherwise, the Software under this license is made available strictly on an “AS IS” BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED. Please review the license for details on these and other terms and conditions.

```



#### Third Party Notices

If your package has third-party elements, you can include the licenses in a Third Party Notices.md file. You can include a **Component Name**, **License Type**, and **Provide License Details** section for each license you want to include. For example:

```
This package contains third-party software components governed by the license(s) indicated below:

Component Name: Semver

License Type: "MIT"

[SemVer License](https://github.com/myusername/semver/blob/master/License.txt)

Component Name: MyComponent

License Type: "MyLicense"

[MyComponent License](https://www.mycompany.com/licenses/License.txt)

```

**NOTE**: Any URLs you use should point to a location that contains the reproduced license and the copyright information (if applicable).
