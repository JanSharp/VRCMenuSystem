
# General UI Design

- Anything intractable in the UI must have some form of feedback
  - Visual feedback when hovering or clicking
  - Toggles represent their current state
  - Potentially UI sounds
    - Volume must be configurable
- Scrolling must not be an annoyance. It should support
  - Using the up and down inputs
  - Dragging the content
  - Dragging the scrollbar (must be wide enough)
  - Maybe experiment with "page down/up" buttons in the UI
- Decent contrast between UI text and its background
- Having some kind of color that indicates something is synced/global that is used everywhere
  - Local would have their own color, or just be white/gray
- Default menu open page
  - Players - Main
  - GMs - Last State
- Things need to be large, as per usual

# Common Tasks

Common tasks are things and workflows that make up like 99% of the usage of the GM menu. Thus features should be built around them to reduce friction and speed up the process.

- [Pings](#pings)
- [Talk Motes](#talk-modes)
- [No Clip](#no-clip)
- [Teleporting](#teleporting)
  - Keeping an eye on a player
  - Keeping an eye on a scene
- [Spawning GM proxies](#spawning-gm-proxies)
- [Spawning items](#spawning-items)

# Uncommon Tasks

- [Summoning players](#summoning-players)
<!-- - [Discreetly telling a player something without others knowing](#discretely-talking-with-players) -->
- Changing global brightness of the map locally, like night vision
- Big maybe changing far clip plane (`VRCCameraSettings.ScreenCamera.FarClipPlane = 100;`)
- Changing UI sound volume
- Accessibility settings panel (might just be the general settings page)
- Haptics settings
- By default it opens the main menu for players
  - Setting to have it remember, which defaults to true for GMs
- To make this a more generic system
  - Permission system, editable at runtime
    - Being able to set which player is GM vs isn't
    - Being able to change what a player can and cannot do
    - Maybe adding custom permission levels
- Time of day controls in some way
- help page

## Pings

For players they are the primary method of getting into contact with a GM.

- Easily accessible
  - As few button presses as possible
  - Should therefore be on the same page as anything else quick and important that should be available to the players
- ~~Maybe state in the UI that pings do not time out~~ Muh clean UI!
- They let the player know when a GM has arrived
  - The ping indicator goes away
    - Blink for about a second before disappearing
  - Big maybe play a sound too
    - Concerns about it being taken in character
    - Would need a setting to toggle it, and it would probably just be an accessibility feature - default off
- Can undo pings, if it was an accident or assistance is no longer needed
  - The ping buttons are toggles, therefore pressing the button again undoes the ping
- Pinging should close the menu (if we go with a menu (probably will))
  - Play (unique) sound
  - Play haptics
- Types of pings
  - Request GM
  - Urgent
- Crazy ideas for requesting GM without pinging
  - Down and trigger on the right controller
    - Consume the trigger input
    - Consume the down input, do not attach items
    - Play (unique) sound
    - Play haptics

For GMs they are a method of communicating with players and other GMs.

- Communicate which players require assistance
  - Undone pings completely disappear. Trust the player in their choice that they did not need assistance
- Provide methods for GMs to assist said players
  - By teleporting to them. That's it, it's the universal and best method of assisting players. Being present
- Focus on the ones which have not received assistance yet
  - Order Urgent to the top
  - Put them to the top of the list, oldest first (the ones that have been waiting the longest)
  - Maybe otherwise highlight them
  - Using color to indicate how long a ping has been sitting there, like 5 minutes red
- Communicate between GMs who is handling what
  - Auto mark pings as handled as soon as teleporting
  - Manually marking pings as handled, for edge cases
  - Option to mark as unhandled, also for edge cases
- Old handled pings are irrelevant
  - can be deleted
- Maybe telling other GMs that you cannot handle a ping
- Maybe number as part of the HUD for open pings

## Talk Modes

- There's different ranges for people to pick from
  - Whisper
  - Quiet
  - Talk
  - Yell
  - Broadcast (GM only)
- On the main page that pings are also on
  - Maybe on a side or bottom row just like Sylan's. Maybe
    - If yes then buttons need to be a bit bigger compared to Sylan's to reduce UX friction
- The mode you are in must have a lot of feedback in order to prevent users getting stuck on a mode without knowing
  - Make the voice icons on the HUD obvious, but not obnoxious
    - Static (as it is in Sylan's menu)
    - Slow throbbing
    - Fast blinking
    - Maybe expose it as a setting in the settings tab (sane defaults are better though)
- The system must not break
  - Hah funny
    - It's using lockstep
    - If any system using lockstep breaks, everything breaks!
- Voice range visualization - a sphere visible on any geometry it intersects
  - Static (as it is in Sylan's menu)
  - Slow throbbing
  - Fast blinking
  - Maybe expose it as a setting in the settings tab (sane defaults are better though)

## No Clip

- Support muscle memory
  - Copy Sylan's logic 1 to 1 for VR
  - wasd qe for desktop
- Speed control
  - Going slow for some things where the GM is visible to the player in some way
  - Going fast like all other times
  - 8 min
  - 32 default
  - 48 max
- Save no clip on off state and speed in player data

## Teleporting

- **General**
  - Undo/Redo teleport, a stack
    - When teleporting to a player, remember both their position and which player it was, so when revisiting that entry in the undo stack it teleports you to where the player is at, not where they were. Same for locations if there is any moving locations
    - Show what will be undone or redone
    - Only record teleports to player, the top of the stack will be your last location
  - Experiment with positioning the teleporting player in a convenient location
    - For GMs positioning the GM in front of an facing the player they teleported to
    - For other usage of teleports facing the same direction as the player being teleported to may be desirable, and relative positioning is currently undefined. This should likely be the default
- **Keeping an eye on a player**
  - Pin players to the top of the player list
    - Finding players by proximity to more quickly pin them
      - Do not re-sort the list automatically when it is sorted this way
  - Finding players by player name
  - Finding players by character name
    - Having multiple characters per player is such an uncommon edge case that it can be ignored
  - Basically all finding of players is covered by the ability to change sort order in the player list
- **Keeping an eye on a scene**
  - Teleporting to locations
    - List of defined locations in the unity editor
    - ~~Maybe runtime placeable locations. Could also be tied into GM proxies. Uncertain. Probably niche~~

## Spawning GM Proxies

## Spawning Items

## Summoning Players

- Selecting players
  - Try point and click selection of players
  - Show count of how many players are selected
  - Option to sort players by selected state
  - Closing the UI does not clear selection
  - Potentially save and load selections
    - have popups for multiple slots
    - grey everything else out while the popup is open, click anywhere to close
  - Select all
  - Select none
  - Invert selection
- Summoning multiple players spreads them out
  - Better than summoning one by one as that'd waste time
  - Prevent players clipping inside each other
  - In a circle around the summoner (must have)
    - Having this as the only option makes UI and UX easier
  - Radius should scale with the amount of players being summoned, such that people are about 1 meter apart from each other
    - Defined as some minimum radius, a maximum radius and a preferred distance between players. Most likely hard coded constants
    - In cases where there are so few players selected such that the circle with minimum radius has a bigger circumference than the total preferred distance between players, do not use the full circle but rather have an arc centered in front of the summoner, with players positioned using the preferred distance from each other. In terms of gaps this would mean all but one gaps between the players would be the preferred distance, the one bigger gap being behind the summoner
  - Upon hitting the summon button ones it prompts for a confirm
    - While waiting for confirmation, a preview of the circle can be shown
  - ~~Maybe an alternative which spreads them out all facing the same direction as the summoner~~
- Visual indictor on the ground where somebody is about to be summoned, local only to the summoner
  - TODO: what if it's not just local though, so everybody can see where that someone is about to be summoned in some location? :thinking:
- Notched vs Free Flowing Scrolling
  - Do free flowing first, think about the other later if desired

<!--
## Discretely Talking With Players

- Also using the player selection system
- While active, only selected players will be able to hear you
- The voice range remains unchanged
-->

## Players Backend

- Deleting player data for offline players
- Deleting selection groups if it ends up not fitting anywhere else
- Set override player names, so you can change names that don't print or sort well

# Relevant Differences To Existing Systems

- No thumbnails for players
- No watch feature
  - Hardly useful in general, especially with undo teleport
- No teleporting back to where you disconnected from
  - An out of character toggle on avatars or invisible avatars plus teleporting to players and locations solves this issue ultimately more elegantly

# Ideas

- Reach into and or touch a scroll view and swipe up and down and it scrolls
  - Take it a step further and have buttons react to touch... effectively have the whole thing behave like a touch screen
- Either have the setting for voice range visualization be a toggle for each voice range (whisper, quiet, talk, yell), or have whisper, quiet and yell grouped together and a separate one for talk
