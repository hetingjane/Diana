# Purpose
The aim of this document is to establish a directory structure for the project that integrates well with Unity asset packages and version control systems.

# Directories
```
_  Assets                               Root directory
 |
 |__ Standard Assets                    Unique location to place Unity Standard Assets. Not version controlled.
 |
 |
 |__ StreamingAssets                    Unique location to place assets that are copied as-is to file system on the target machine
 |                                      that can be accessed in scripts as Application.streamingAssetsPath. Version control selectively.
 |
 |__ Editor Default Resources           Unique location to place asset files available for editor scripts via EditorGUIUtility.Load(). 
 |                                      Assets can be in subfolders. Excluded from builds. For my purposes, editor scripts will expect
 |                                      resources in their own Resources folder, therefore, this won't be version controlled.
 |
 |__ Gizmos                             Unique location to place asset files (usually graphics) available for Gizmos via Gizmos.DrawIcon().
 |                                      Assets can be in subfolders. Excluded from builds. Version control selectively.
 |
 |__ Plugins                            Unique location to place external (usually pre-compiled) scripts that may be managed assemblies or
 |                                      natively compiled, platform-specific binaries. Version control selectively.
 |
 |__ ThirdPartyPackage1                 Third party asset packages are installed via Asset Store or repository clones. Not version controlled.
 |
 |__ ThirdPartyPackage2
 |
 |__ ThirdPartyPackage3
 |
 |__ ...
 |
 |
 |__  _Core                             Core directory to house all the assets that must be present for scenes to work.
    |                                   Everything under this directory is assumed to be under version control.
    |                                   
    |__ Editor                          Editor scripts to add functionality to the editor during development. 
    |  |                                
    |  |
    |  |__ EditorFunctionality1         Scripts can be in subfolders. These are not available in builds at runtime.
    |  |  |
    |  |  |__ Resources                 Asset files for editor scripts. Excluded from builds.
    |  |
    |  |__ EditorFunctionality2
    |  |
    |  |__ ...
    |
    |
    |__ Resources                       Assets that can be loaded on-demand from a script using Resources.Load() instead of creating a scene object.
    |                                   Assets can be in subfolders. Unity cannot strip any unused assets in there i.e. it cannot tell if you load
    |                                   it via code or not) so everything in that folder will be included in the final build. Avoid, if possible.
    |                                    
    |__ Animation                      
    |  |
    |  |__ Animations                   Animation files (.anim)
    |  |  |
    |  |  |__ modelName@animation.fbx   Naming convention recommended by Unity if model data and animation data come as separate files 
    |  |  |__ model.mb                  Using this convention will allow Unity to set references to the animation files in the model automatically.
    |  | 
    |  |
    |  |__ Controllers                  Animator Controllers (.controller)
    |
    |__ Audio
    |
    |__ Models
    |
    |__ Materials                       Place Material (.mat) assets here.
    |
    |__ PhysicsMaterials                Place PhysicMaterial (.physicMaterial) assets here.
    |
    |__ Shaders
    |
    |__ Prefabs
    |
    |__ Scripts
    |
    |__ Fonts
    |
    |__ Textures
    |
    |__ Sprites
    |
    |__ Scenes
    |
    |__ Prototyping                     Place prototype assets and scenes here. Don't reference these assets in non-prototype scenes.
```

Note that all the leaf directories under `_Core` assume subdirectories to group assets by object or scene. For example, `Assets/Materials/Level1`,  `Assets/Prefabs/Enemies`, etc.

# Scene Hierarchy
- Prefer multiple shallow hierarchies at the root to one deep hierarchy. Reason: Multiple hierarchies enable multi-threading for transform changes, plus avoiding them for unrelated children objects. Exception is static objects, since they by definition won't have their transform changed often, if at all.
- Do not make one game object a child of another unless it needs to move with it. This also applies for dynamically create game objects.
- Use *Optimize GameObject* in the Rig tab of your Animation import settings. This will prevent the "skeleton" GameObject of your rig being created, reducing the hierarchy. If you need to still expose some (you may use one as the target to attach equipment on the character) you can manually pick it to only expose this one.
- Use empty GameObjects as *headers*. For example, a game object named `--Lights--` could be placed in the hierarchy, not as parent of Light objects, but just above it as a sibling object to delimit it from other type of objects. Other possible header game objects could be Cameras, World, Management, GUI, etc.
- All empty objects should be located at `0,0,0` with default rotation and scale.
- For empty objects that are only containers for scripts, use `@` as prefix – e.g. `@Cheats`.
- Put your world floor at `y = 0`. This makes it easier to put objects on the floor, and treat the world as a 2D space (when appropriate) for game logic, AI, and physics.
- Put character and standing object pivots at the base, not in the center. This makes it easy to put characters and objects on the floor precisely. It also makes it easier to work with 3D as if it is 2D for game logic, AI, and even physics when appropriate.
- Use `1 unit = 1 meter`, which is necessary anyway for Unity Physics engine, along with a reference cube to scale everything appropriately.
- Put dynamic objects under a parent named 'Dynamic Objects' to avoid cluttering your scene hierarchy.

# Immutability Constraint
All third-party packages should be treated as immutable. This means either use such assets unmodified. Or, move a duplicate to a location under `_Core` directory before modifying it.

# Version Control Settings
In the Unity Editor, go to `Edit > Project Settings > Editor` and set:
- **Version Control.Mode** to **Visible Meta Files**: This will place meta files next to your assets, in which parameters and import settings are stored. This allows you to share import settings and references by including these files in version control. Otherwise, these settings cannot be shared between members of the team.
- **Asset Serialization.Mode** to **Force Text**: This will make all unity files (ScriptableObjects, Scenes, Prefabs and many others) use a text representation instead of a binary one. This allows source control to just store modifications to these files when they change, rather than a whole new copy of the file, and allows you to manually merge any conflicts.
- **Line Endings for New Scripts.Mode** to **Windows**: This makes sure that script files consistently have CRLF line endings.
- Line normalization in Git: Make sure `git config core.autocrlf` is set to `true` on Windows, and `input` on UNIX. This makes sure that checked-in text files have LF endings. Checked-out code will preserve line endings as-is on Unix, but will be converted to CRLF on Windows.
- Prepare your clone for Git LFS by executing `git lfs install` in your repository.
- A ready made `.gitattributes` file for Unity projects is tracked [here](https://gist.github.com/bangar1991/7500a606f6dc27ae80cdcef93a5a4cfb). 

# Requirements Documentation
Always document the packages needed along with their Unity Asset Store URLs or repository URLs in `README.md`.

# References:
- [Large Project Organisation](https://unity3d.com/learn/tutorials/topics/tips/large-project-organisation)
- [7 Ways to Keep Your Unity Project Organized](https://blog.theknightsofunity.com/7-ways-keep-unity-project-organized/)
- [Best practices - Folder structure](https://forum.unity.com/threads/best-practices-folder-structure.65381/)
- [50 Tips for Working with Unity](http://devmag.org.za/2012/07/12/50-tips-for-working-with-unity-best-practices/)
- [Mastering Unity Project Folder Structure](http://developers.nravo.com/mastering-unity-project-folder-structure-level-2-assets-organization/)
- [Special folder names](https://docs.unity3d.com/Manual/SpecialFolders.html)
