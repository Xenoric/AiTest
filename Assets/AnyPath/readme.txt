Thank you for purchasing AnyPath!

Version 1.6

The package has been updated to work out of the box with Unity 6.
Previous versions also supported this, but sometimes required manual renaming of NativeMultiHashmap in order to work.
All of the warnings that resulted from using AnyPath with later Untiy versions have also been addressed.

Using this release with older versions of Unity (6) Burst (1.8.18), or Collections (2.5.1) may require
renaming NativeParallelMultiHashMap to NativeMultiHashMap. Other than that, it should work fine.
Please contact anypath@bartvandesande.nl if you encounter any problems.

Version 1.5

** Upgrade Notes **

This version defines a new method "ModifyEdgeBuffer" on IEdgeMod.
If you have custom implementations of IEdgeMod in your project, this will give a compile error because that method needs to be implemented. 
You can implement this method and keep it empty and everything will work. 
There will be no additional overhead in builds because the Burst compiler is smart enough to strip unused code.

Version 1.4.4

** Known issues **

-----

If you're getting a compilation error along the lines of:
"The type or namespace name 'NativeMultiHashMap<,>' could not be found."

This package was made with Unity 2020.3.16f1, so that it remains compatible with older Unity versions.
The error occurs because Unity renamed these collections. NativeMultiHashMap became NativeParallelMultiHashMap in later versions.
This *should* be resolved automatically, but if it doesn't, you can fix it by running the API updater by launching
Unity with the -accept-apiupdate option.

For more information visit: https://docs.unity3d.com/6000.0/Documentation/Manual/APIUpdater.html

-----

In some extremely rare cases, if you get a warning along the lines of:
"Compilation was requested for method ..." for a PathFinder job. 
First check if you made a concrete implementation of the class using the AnyPath code generator.

If so, then there may be a bug in Unity where the Burst compiler sometimes doesn't detect these classes if they are defined
alphanumerically before the AnyPath folder itself. I know this sounds very strange, but it currently is the only explanation for
this issue. Please try renaming the class that your definitions are in and see if it resolves the warning.
You can also try running a build and see if the error "Job was not burst compiled!" turns up. If you do not get this error then everything
is OK and AnyPath should run optimal.

-----

** Upgrade notes (1.4.1) **
The way ALT heuristics are generated has slightly changed in order for it to actually burst compile in builds.
See the documentation and the square grid example for details.

In previous versions, the Path result class had a Segments property. This has been removed
to allow for zero allocation results. The segments of a path can now only be obtained by using the indexer on the class itself.

Check out the demos and read the documentation at: 
https://anypath.bartvandesande.nl

For questions regarding AnyPath feel free to reach out to me at:
anypath@bartvandesande.nl

Installation requirements:
- Unity 2020.3 or higher
- Burst minimum version 1.4.11
- Unity Collections minimum version 0.15.0
If your project uses the Entities package, Unity Collections will already be included

Import AnyPath.unitypackage into your project.
If you encounter any compilation errors, you may need to install these packages manually.

- Click on Window -> Package Manager
- On the packages dropdown, select Packages: Unity Registry
- Locate Burst and install the latest version
- Click on the + > Add package from GIT url
- Enter com.unity.collections and hit ENTER