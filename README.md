This is the source code for Axen's Performance Enhancements (APE). Credit to u/Admiral_Turboclown for the naming ideas!

When I say AI did not create this mod, that does not mean I'm anti-AI or refuse to use it during development. I've been experimenting with AI in programming ever since ChatGPT came out in 2023 and it wasn't until about a year ago where I've started to take its output seriously. Of course I still enjoy writing my own code but AI has become helpful tool for debugging and running tests, which is my main use case for it. If you disagree with how I use AI then I'm sorry but I will not stop, and you have no obligation to use anything I develop if that's the case. I guess the clear answer to "do I use AI" is I do not blindly generate code with AI (I'm looking at you, vibe-coders), but I don't exclude it from my development process. I won't use AI for designing art assets, however. And if anyone's curious, I mainly use GPT-6.1 Sol in Codex. "But bro where's your 20-agent parallel workflow with 30 verification stages and 20 iterations??" like cmon I'm just using AI to better test my shit in-game and to help fix my buggy ass code, not create Minecraft 2. Sorry for the rant

The options in the experimental section of mod settings are features I'm actively working on and testing, so they are not feature-complete and don't guarantee performance improvements.

##Recent news - October 19 @9:48PM

Benchmarking implies one or more of the experimental optimizations I created are actually regressing rather than improving TPS, at least in the early-game. I'll do mid and late-game colony benchmarking tomorrow morning after I go surfing.

The 30,000-tick tests were run 5 times per group, and the 12,000-tick tests were run 3 times per group.

This benchmark was with an early-game colony of just 1 colonist.

Base TPS group is Core + DLCs.

###Latest benchmarking results with Standard optimizations:

- (30,000 ticks passed) | Base TPS = 5,945 | Standard On = 6,797 | +14.3% TPS
- (12,000 ticks passed) | Base TPS = 3,824 | Standard On = 4,525 | +18.3% TPS

###Latest benchmarking results with Experimental + Standard optimizations:

- (30,000 ticks passed) | Base TPS = 6,026 | E+S = 6,545 | +8.6% TPS
- (12,000 ticks passed) | Base TPS = 4,481 | E+S = 4,788 | +6.8% TPS

Benchmarking was done on a MacBook Air 15-Inch with the base Apple M5 chip and 16GB RAM running on MacOS Tahoe 26.5.1

Check out my other mods on [Steam Workshop.](https://steamcommunity.com/profiles/76561199797667982/myworkshopfiles/?appid=294100)