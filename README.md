# TinyBasic compiler on C#
An implementation of a design note variant of Tiny Basic language with some additional features, fully written in C#.

# Features
* Support of 12 statements (LET, IF, PRINT, INPUT, GOTO, GOSUB, END, RETURN, CLEAR, LIST, RUN, REM).
* Terminal mode — just write your code in terminal and get immediate response...
* ... or load your .bas files and execute them!
* No Nuget dependencies (except ones for unit testing).
* Basic manual is included in 'help' command.
* A fully functional console debugger! 

# Some *old* thoughts and future plans (both short- and long-term)
>Add a debugger that supports step-by-step execution, breakpoints, stack call preview and maybe some other features. 

Done! this interpreter now have a functional console debugger with basic functionality such as breakpoints, step by step execution and other features!
>Do better error handling. I don't like it in it's current form.

Done? Kind of? A major refactoring was done which changed for the better (I think) error handling, but I still think that it can be done much better.
>While I'm fairly satisfied with most of the code base, there's a lot of room for improvement.

Right now, there is a lot of room to improvements and optimization, but I don't think that the main architecture will drastically change (except for the error handling). 
>Add support for other keywords (like REM and RND).

Done! Interpreter now supports a REM keyword (which wasn't hard to implement since it was a comment) and has a base for a functions such as RND which also was implemented.
>It currently supports running code from a file, but has no saving feature.

Done! You can now load AND save any programs you write!
> I'm terrible at writing unit tests and don't like them in their current state.

Unit tests was the part I wasn't satisfied with the most. While implementing debugger, I decided to abandon the idea of unit testing for this project. Writing them was a really tedious at best (sisyphean at worst) task that **sometimes** helped me catch some pesky bugs. Maybe this isn't the best project for unit tests, maybe it's a skill issue, or maybe a combination of both that led me to my final decision.

# Some new thoughts!
* There is not many *new* things in particular that I want to add. Currently, this interpreter have all the functionality that was planned in the beginning.
* I will still do some optimizations, bugfixes, cleanups and other minor things here and there without changing current architecture <p>. . . . except for the error handling probably. I don't really like the exception-based handling right now, although I'm not really sure how do it better for now.

# Information sources
Ideas and inspiration come from following:
* "TINY BASIC User Manual" from Itty Bitty Computers.
This article is the main source of knowledge for this project.
* "BUILD YOUR OWN BASIC" from Dr. Dobb's Journal.
The syntax has been copied (with some additions) from original design note published in this journal.
* "Let's make a Teeny Tiny compiler" series by Austin Z. Henley
This is my first time writing my own compiler. This series helped me to get a general idea of how a compilers works.
* "How debuggers work" series by Eli Bendersky. 
A great series to get the general idea of how a debuggers work.