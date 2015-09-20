# isosurface
A project testing and comparing various algorithms for creating isosurfaces. 

In an effort to find the best way of making a dynamic voxel engine, I've been researching new methods of extracting isosurfaces from data sets. I'm making my findings open-source. Some of the code is borrowed/inspired by existing implementations, but most is my own. It's not very optimized and it doesn't have much of a use outside of displaying data, but maybe you'll find some use in it.

**Implemented and Mostly Working Algorithms**
* 2D Uniform Dual Contouring
* 2D Adaptive Dual Contouring
* 3D Uniform Dual Contouring
* 3D Adaptive Dual Contouring

**Notes**
* The 2D implementations don't connect properly; they were my first tests with DC and have some issues, and the adaptive implementation doesn't have simplification
* My QEF solver is hardly a QEF solver; Rather, it takes a set of offsets to apply to the mass point and find the one that has the lowest error, so do **not** use this as an example of what to do
* The QEF solver is disabled in the 3D implementations but can be enabled (simplification needs some tweaks); sharp features will be lost and mesh will be deformed if the error tolerance is high without it

![Adaptive dual contouring](http://i.imgur.com/7LLNFut.png)
