# UnityExtensions.VSCodeIntegrationPatch

Fixes two issues that prevent the `C# for Visual Studio Code` plugin from providing Intellisense and refactoring features when editing your Unity project's `C#` scripts.

## 1. Malformed Solution File
Some versions of Unity generate a solution file, which are not accepted by `msbuild`.  Specifically, Unity may write the project entries in the following format, where "Example" is the name of the Unity project, and the solution file is `Example.sln`:

```
Project("{...}") = "Example", "UnityExtensions.ArrayDrawer.csproj", "{...}"
EndProject
Project("{...}") = "Example", "Assembly-CSharp.csproj", "{...}"
EndProject
Project("{...}") = "Example", "UnityExtensions.ArrayDrawer.Editor.csproj", "{...}"
EndProject
Project("{...}") = "Example", "Assembly-CSharp-Editor.csproj", "{...}"
EndProject
```

Running `msbuild Example.sln` on a solution with this format produces the following error message:

```bash
Example.sln : Solution file error MSB5004: The solution file has two projects named "Example".
```

Whenever Unity writes the solution file, this library will rewrite the project entries in the following format, which is accepted by `msbuild`:

```
Project("{...}") = "UnityExtensions.ArrayDrawer", "UnityExtensions.ArrayDrawer.csproj", "{...}"
EndProject
Project("{...}") = "Assembly-CSharp", "Assembly-CSharp.csproj", "{...}"
EndProject
Project("{...}") = "UnityExtensions.ArrayDrawer.Editor", "UnityExtensions.ArrayDrawer.Editor.csproj", "{...}"
EndProject
Project("{...}") = "Assembly-CSharp-Editor", "Assembly-CSharp-Editor.csproj", "{...}"
EndProject
```

## 2. VSCode/Omnisharp Does Not Use Unity-Compiled Assemblies
To avoid running `msbuild` to recompile all of the Unity project's assemblies for Omnisharp, this library rewrites the Unity-generated C# Project files by replacing this:

```
    <OutputPath>Temp\bin\Release\</OutputPath>
```
with this:
```
    <OutputPath>Library\ScriptAssemblies\</OutputPath>
```
This change enables Omnisharp to load the assemblies generated by the Unity editor.