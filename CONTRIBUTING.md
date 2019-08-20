# Summary
This file summarizes any code conventions being followed in the individual projects and Git in general.

## Git
### Write a good Git commit message
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

## BlocksWorld

### Modules
Modules are the preferred way to communicate with `DataStore`.
- A module class should always a `Module` suffix.
