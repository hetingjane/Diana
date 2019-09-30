# Summary
This file summarizes any code conventions being followed in the individual projects and Git in general.

## Git

### Small commits
A commit should only have related changes. Any time you find yourself using a `;` or `and` in your commit message, think if you can separate the commit into multiple commits. This doesn't mean that small commits can't change multiple files. In fact, they often do. However, the focus of the commit should always be that _one_ bug fix or feature addition.

Small commits are easy to revert, and reduce project maintenance pain in the long run.

### Write a good Git commit message

![xkcd_git_commits](https://imgs.xkcd.com/comics/git_commit.png)

- **Limit the subject line to 50 characters** Keeping subject lines at this length ensures that they are readable, and forces the author to think for a moment about the most concise way to explain what’s going on. However, consider 72 the hard limit.

- **Do not end the subject line with a period** Trailing punctuation is unnecessary in subject lines. Besides, space is precious when you’re trying to keep them to 50 chars or less.

- **Capitalize the subject line** Begin all subject lines with a capital letter.

- **Use the imperative mood in the subject line** Imperative mood just means “spoken or written as if giving a command or instruction”. For example, clean your room, close the door, etc. Git itself uses the imperative whenever it creates a commit on your behalf. For example, _Merge branch 'myfeature'_, _Revert "Add the thing with the stuff"_, etc. As a rule of thumb, a properly formed Git commit subject line should always be able to complete the following sentence: _If applied, this commit will &lt;your subject line here&gt;_. Use of the imperative is important only in the subject line. You can relax this restriction when you’re writing the body.

- **Separate subject from body with a blank line** Not every commit requires both a subject and a body, especially if the change is very simple that unnecessitates a context. For such changes, `git commit -m <message>` works fine. For complex changes though, use `git commit` and in the text editor that opens after (setup as `git config --global core.editor <editor>`), write a subject as well as a body that must be separated by a single blank line.
  ```
  <subject>

  <body>
  ```
  With long commit messages like these, it becomes necessary to use `git log --oneline` to get a summary of git commits.


- **Wrap the body at 72 characters** Git never wraps text automatically. When you write the body of a commit message, you must mind its right margin, and wrap text manually. The recommendation is to do this at 72 characters. A good text editor that can be setup to do this automatically for git commits can be really helpful. For example, for GNU nano, you can do something like `git config --global core.editor "nano -r 72"` which sets up hard-wrapping at 72 characters.

- **Use the body to explain what and why vs. how** In most cases, you can leave out details about how a change has been made. Code is generally self-explanatory in this regard (and if the code is so complex that it needs to be explained in prose, that’s what source comments are for). Just focus on making clear the reasons why you made the change in the first place—the way things worked before the change (and what was wrong with that), the way they work now, and why you decided to solve it the way you did.

Source: [chris.beams.io](https://chris.beams.io/posts/git-commit/)

### Merging your work
Git does a pretty good job at merging normal source files. However, merging YAML serialized scene files is tricky. There are two ways to do this:

- Manual scene merges
- Automatic scene merges (Unity YAML Merge tool)

#### Manual scene merges
This works by modifying the scene version in the main branch so as to recreate the one present in your feature branch. For example, by adding in missing game objects, modifying existing ones, removing, etc. The assumption is you know how to recreate the scene version in your feature branch.

- `git checkout develop` # or the branch you want to be merged into
- `git merge feature_branch` # the feature branch we want merged in

If the scene versions differ in the two branches, there will be either a merged scene file or a merge conflict for the scene file. Either way:

- Replace the merge result/conflict with the `develop` branch's version before the merge
`git checkout HEAD -- <path_to_scene_file>`
- Make the scene modifications necessary to recreate the scene version in your feature branch
- Verify the scene works in Unity, esp. the functionality you implemented in your feature branch.
- Mark the manually created scene as merged
`git add -- <modified_scene>`
- Resolve other merge results/conflicts.
- `git commit` # to conclude the merge

#### Unity YAML Merge tool
- Unity comes with [SmartMerge tool](https://docs.unity3d.com/Manual/SmartMerge.html) to properly merge YAML serialized files.
- To use SmartMerge, you should set it as a Git Merge driver as:
  ```
  git config --local merge.unityyamlmerge.driver "'<path_to_smart_merge>' merge -h -p --force %O %B %A %A"
  git config --local merge.unityyamlmerge.recursive binary
  git config --local merge.unityyamlmerge.name "Smart Merge"
  ```
- The `.gitattributes` is already configured to use `unityyamlmerge` merge driver for scenes, prefabs, etc. so the above driver will be invoked automatically whenever you do a merge.
- Additionally, you can modify the `mergespecfile.txt` to enable a fallback merge tool to be run when SmartMerge tool detects merge conflicts. For example, in case of [Meld](http://meldmerge.org) (you can create a similar rule for your own choice of merge tool):
  ```
  * use "%programs%\Meld\Meld.exe" "%l" "%b" "%r" --output "%d"
  ```
  Alternatively, you could achieve the same effect by configuring a merge tool in Git itself instead of `mergespecfile.txt`:
  ```
  git merge.tool meld
  git mergetool.meld.path <path_to_meld.exe>
  git mergetool.meld.prompt false
  git mergetool.meld.keepBackup false
  git mergetool.meld.keepTemporaries false
  ```
  However, the latter means you have to invoke `git mergetool` after `git merge` if there are conflicts to start the merge tool.
- In our experience, Unity YAML merge may not always do the right thing, so it's always better to test if the merge result is acceptable or not.

## BlocksWorld

### Modules
Modules (i.e. subclasses of `ModuleBase`) are the preferred way to communicate with `DataStore` (the blackboard).
- A module class name should always end with a `Module` suffix.
- Header comments should briefly explain what the module does, and list the blackboard values that the module reads and writes.
- If a class can't derive from `ModuleBase` (for example because it needs to subclass something else for important functionality), that's OK.  Note what blackboard values it reads and writes in the header anyway.

### Capitalization
- Class and method names should always begin with an uppercase letter: `SampleClass`, `SampleMethod`.
- Field/property names should begin with lower case: `sampleProperty`.
