This is the source code for Axen's Performance Enhancements (APE). Credit to u/Admiral_Turboclown for the naming ideas!

When I say AI did not create this mod, that does not mean I'm anti-AI or refuse to use it during development. I've been experimenting with AI in programming ever since ChatGPT came out in 2023 and it wasn't until about a year ago where I've started to take its output seriously. Of course I still enjoy writing my own code but AI has become helpful tool for debugging and running tests, which is my main use case for it. If you disagree with how I use AI then I'm sorry but I will not stop, and you have no obligation to use anything I develop if that's the case. I guess the clear answer to "do I use AI" is I do not blindly generate code with AI (I'm looking at you, vibe-coders), but I don't exclude it from my development process. I won't use AI for designing art assets, however. And if anyone's curious, I mainly use GPT-6.1 Sol in Codex. "But bro where's your 20-agent parallel workflow with 30 verification stages and 20 iterations??" like cmon I'm just using AI to better test my shit in-game and to help fix my buggy ass code, not create Minecraft 2. Sorry for the rant

The options in the experimental section of mod settings are features I'm actively working on and testing, so they are not feature-complete and don't guarantee performance improvements.

## Update - October 21 @11:48AM

Sorry if development or updates seem slow, I like playing RimWorld as much as I like modding it.

## Update - October 20 @5:35PM

Welp, my benchmarks might just be shit, at least in the way I'm testing. I did more than just 1-colonist testing and started doing early-game (3 colonists), mid-game (10 colonists), and late-game (25 colonists) scenarios. Unfortunately the control group (Core + DLCs) has too wild of results to act as a reliable comparison. For example, having only Harmony alongside the control group reduced average TPS by about 600 ticks across testing which doesn't seem right at all. I was doing an A/B approach where I enable only the first half of experimental options in the A group and second half in the B group, and originally, the results implied that B was causing more of a regression. However, I feel inclined to try and improve consistency across the whole benchmarking process before I deduct anything from those resuls. My laptop is plugged in, all other apps are closed other than RimWorld of course, and I avoid using the computer during any test runs. I think this points to an actual variation in the game runtime, but I still need to check.

## Recent news - October 19 @9:48PM

Benchmarking implies one or more of the experimental optimizations I created are actually regressing rather than improving TPS, at least in the early-game. I'll do mid and late-game colony benchmarking tomorrow morning after I go surfing.

The 30,000-tick tests were run 5 times per group, and the 12,000-tick tests were run 3 times per group.

This benchmark was with an early-game colony of just 1 colonist.

Base TPS group is Core + DLCs.

### Latest benchmarking results with Standard optimizations:

- (30,000 ticks passed) | Base TPS = 5,945 | Standard On = 6,797 | +14.3% TPS
- (12,000 ticks passed) | Base TPS = 3,824 | Standard On = 4,525 | +18.3% TPS

### Latest benchmarking results with Experimental + Standard optimizations:

- (30,000 ticks passed) | Base TPS = 6,026 | E+S = 6,545 | +8.6% TPS
- (12,000 ticks passed) | Base TPS = 4,481 | E+S = 4,788 | +6.8% TPS

Benchmarking was done on a MacBook Air 15-Inch with the base Apple M5 chip and 16GB RAM running on MacOS Tahoe 26.5.1

## Shower Thoughts

Oct 20 @12:05AM It's late at night, but would it be worth it to try creating a flow field for groups of pawns in RimWorld? The obvious hurdles are that pathfinding targets are constantly updating, and I don't even know how you'd keep close to vanilla pathfinding results. If a raid had a flow field they would probably all just pool into a single door which is neither vanilla...actually, vanilla does kind of do that when you got a killbox. idk. I'm not even sure if it'd be possible, and I know the "hybrid" approach (like regional flow fields rather than each individual pawn) is always an option but I'm afraid I risk creating the whole system and then the overhead exceeds that of just normal vanilla pathfinding. I'll get some sleep first and then throw some more mental energy at it eventually.

Oct 20 @12:40AM Ok, last one for tonight. Crazy thought, but I recently saw on Reddit that someone built a tool to be able to make RimWorld mods in Rust. Now, that's basically all I know, idk if it helps improve performance in the FFI for Rust <-> C# or if it makes it easier to package your Rust code with the C# in the mod .dll or if it might even entirely remove the need to use C# — I'm not sure. My earlier thought was on the topic of pathfinding and I've already made some pathfinding optimizations within this very mod, but what if you rewrote the pathfinding in Rust? Crazy idea, I know, and it all but screams of AI slop since I feel Rust is starting to become associated with vibe-coding. But, hear me out. Rust is fast (that's quite literally one of its main marketing points), faster than C# (although, .NET 10 is pretty good so idk). I have a theory: translating the existing C# pathfinding code into Rust could simply just add free performance! (not literally "free", just more efficient caching and all that shit Rust has) Don't forget though that FFI overhead could cover up any possible gains. Biggest immediate issue is that I only dabble in Rust and I do not have the expertise to manage something like that, so I'd have to rely more on AI output which I'm not completely fond of.

Check out my other mods on [Steam Workshop.](https://steamcommunity.com/profiles/76561199797667982/myworkshopfiles/?appid=294100)
