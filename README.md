# Wayland.SourceGenerator
A source generator that generates bindings for libwayland 

### Introduction
This source generator generates bindings for libwayland for writing native wayland clients in C#.
For further documentation of wayland protocols, see https://wayland.app/.

### Usage

Either install the NuGet package `Wayland.SourceGenerator` or clone the git repository and add a project reference to the source generator in your `.csproj`:

```xml
<ItemGroup>
    <ProjectReference Include="./Wayland.SourceGenerator/Wayland.SourceGenerator/Wayland.SourceGenerator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

Then add the xml definitions as `AdditionalFile`s to your project:

```xml
<ItemGroup>
    <AdditionalFiles Include="WaylandXml/*.xml" />
</ItemGroup>
```

Now you can connect to the wayland socket with
```csharp
WlDisplay display = WlDisplay.Connect();
```
and start writing your client.

> [!NOTE]
> Make sure that all dependencies of a protocol are also included, e.g. xdg_shell needs the core wayland protocol as it uses the `wl_seat` type.

> [!NOTE]
> Duplicate protocols with the same name e.g. when cloning the wayland-protocols repository, `wayland-protocols/unstable/xdg-shell/xdg-shell-unstable-v6.xml` and `wayland-protocols/stable/xdg-shell/xdg-shell.xml` are in conflict.
