# CrypTool2-standalone-PoC

Proof of concept CT2 standalone components projects.

# How it works

The method through which this repository works together with CrypTool2 is through relative paths in the project configuration files (`csproj`). It looks [like this](https://github.com/simlei/CrypTool2-standalone-PoC/blob/4bb7dd819a0248e34b06aa204161f15860caa44f/ComponentStandalone/ComponentStandalone.csproj#L66):

Assuming the CrypTool2 repository is checked out with SVN into the default directory name "CrypTool2", and this repository is checked out right next to that, we get:

```
.
├── CrypTool2
│   ├── CrypBuild
│   └── trunk                       (contains all of CT2 code base)
├── CrypTool2-standalone            (this repository's clone)
```

In this repository, there is the project `ComponentStandalone` which bridges the CT2 and this repository by referencing an isolated part of its source, building a minimal common base. In CrypTool2, there is a file "trunk\CrypPluginBase\Utils\StandaloneComponent\StandaloneComponents.cs" and it represents the ComponentAPI on a minimal level. It in itself has no dependencies on CT2 classes. It is referenced in the `ComponentStandalone` project of this repository like this:

In the project definition `ComponentStandalone\ComponentStandalone.csproj`:
```cs
<Compile Include="..\..\CrypTool2\trunk\CrypPluginBase\Utils\StandaloneComponent\StandaloneComponents.cs" />
```

Completing the references that are at this time necessary is the Project `CrypTool2Common` which references other sources from CT2. They are only a few and not strictly necessary for this all to work in the end. After all, this is a first proof of concept.

# Project structure explained

These are the interesting files in this project; prominently, no `.cs` source files have been left out and all projects are listed here.

```
.
├── ComponentStandalone                    (1)
├── CrypTool2Common                        (2)
├── ComponentStandaloneUtil                (3)
├── LFSRStandalone                         (4)
│   ├── Program.cs                         (4.1)
```



**`├── ComponentStandalone                    (1)`**

As explained before, these hosts the absolutely necessary file that defines a component's interface. This project has no source files itself but the one referenced from CrypTool2 with the path `CrypTool2\trunk\CrypPluginBase\Utils\StandaloneComponent\StandaloneComponents.cs`.


**`├── CrypTool2Common                        (3)`**

This also purely references some other CT2 files, which are not really vital (with some refactoring) for this kind of "Stand-alone components" concept to work. Some common code base with CT2 is convenient, though, and at this time, this common code base consists of:

```
    <Compile Include="..\..\CrypTool2\trunk\CrypPluginBase\Utils\Logging.cs" />
    <Compile Include="..\..\CrypTool2\trunk\CrypPluginBase\Utils\ObjectDeconstruct.cs" />
    <Compile Include="..\..\CrypTool2\trunk\CrypPluginBase\Utils\Datatypes.cs" />
```

**`├── ComponentStandaloneUtil                (2)`**

This project contains helpers to make a component a stand-alone CLI program. For example, it provides classes that allow putting data into the inputs of a component in a fashion that is convenient for testing. The demonstration CLI program is actually testing the LFSR component with two example test cases; it is not far from there to parsing arguments and using them as input, though.

**`├── LFSRStandalone                         (4)`**
 
This proves the concept by taking the "Core" implementation of the LFSR component from CT2 and making it stand-alone. To that end, the file `CrypTool2\trunk\CrypPlugins\LFSR\Core\LFSRImplementation.cs` is referenced from the CT2 repository. That means, this stand-alone project directly reflects the implementation of LFSR as it is in CT2.

Other stand-alone configurations need only add one other project like LFSRStandalone inside this solution. Instead of linking a file from CT2, one could even start in this repository with implementing something like `LFSRImplementation.cs`, test it and then move the file to CT2 and complete the wiring of the components to CT2. I would expect this to have a positive impact on testing and general development speed.

**`│   ├── Program.cs                         (4.1)`**

This wraps the `LFSRImplementation.cs` classes into the helpers from the other projects of this repository and defines some test cases which it exemplarily prints to stdout (and to the VS console).


# Screenshot

This is how the standalone program looks like in VS. Keep in mind that this is all that has been written specific to LFSR in this repository -- all else (like LFSRImplementation) is required to be written in any case. All that would be required is to adhere to the standalone component interface in something like `YourComponentImplementation.cs` which is mirroring the traditional API (just with zero dependencies).

![/dpc/Program_cs_snip.png]
