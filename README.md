# TinyBasic compiler on C#
An implementation of a design note variant of Tiny Basic language with some additional features, fully written in C#.

# Features
* Support of 12 keywords (PRINT, IF-THEN, GOTO, GOSUB, INPUT, LET, RETURN, CLEAR, LIST, RUN, END).
* Terminal mode — just write your code in terminal and get immediate response...
* ... or run your .bas file with file execution.
* No Nuget dependencies (except ones for unit testing).
* Basic manual is included in 'help' command.

# Some thoughts and future plans (both short- and long-term)
* Add a debugger that supports step-by-step execution, breakpoints, stack call preview and maybe some other features.
* Do better error handling. I don't like it in it's current form.
* While I'm fairly satisfied with most of the code base, there's a lot of room for improvement.
* Add support for other keywords (like REM and RND).
* It currently supports running code from a file, but has no saving feature.
* I'm terrible at writing unit tests and don't like them in their current state.

# Information sources
Ideas and inspiration come from following:
* "TINY BASIC User Manual" from Itty Bitty Computers.
This article is the main source of knowledge for this project.
* "BUILD YOUR OWN BASIC" from Dr. Dobb's Journal.
The syntax has been copied (with some additions) from original design note published in this journal.
* "Let's make a Teeny Tiny compiler" series by Austin Z. Henley
This is my first time writing my own compiler. This series helped me to get a general idea of how a compilers works.
