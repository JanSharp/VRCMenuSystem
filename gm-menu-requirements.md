
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

# Common Tasks

Common tasks are things and workflows that make up like 99% of the usage of the GM menu. Thus features should be built around them to reduce friction and speed up the process.

- [Pings](#pings)
- [No Clip](#no-clip)
- [Teleporting](#teleporting)
  - Keeping an eye on a player
  - Keeping an eye on a scene
- [Spawning GM proxies](#spawning-gm-proxies)
- [Spawning items](#spawning-items)

# Uncommon Tasks

- [Summoning players](#summoning-players)
- [Discreetly telling a player something without others knowing](#discretely-talking-with-players)
- Changing global brightness of the map locally

## Pings

For players they are the primary method of getting into contact with a GM.

- Easily accessible
  - As few button presses as possible
  - Should therefore be on the same page as anything else quick and important that should be available to the players
- They let the player know when a GM has arrived
  - The ping indicator goes away
  - Maybe play a sound too
- Can undo pings, if it was an accident or assistance is no longer needed

For GMs they are a method of communicating with players and other GMs.

- Communicate which players require assistance
  - Undone pings completely disappear. Trust the player in their choice that they did not need assistance
- Provide methods for GMs to assist said players
  - By teleporting to them. That's it, it's the universal and best method of assisting players. Being present
- Focus on the ones which have not received assistance yet
  - Put them to the top of the list, oldest first (the ones that have been waiting the longest)
  - Maybe otherwise highlight them
- Communicate between GMs who is handling what
  - Auto mark pings as handled as soon as teleporting
  - Manually marking pings as handled, for edge cases
  - Option to mark as unhandled, also for edge cases
- Old handled pings are irrelevant
  - can be deleted

## No Clip

- Support muscle memory
  - Copy Sylan's logic 1 to 1 for VR
  - wasd qe for desktop
- Speed control
  - Going slow for some things where the GM is visible to the player in some way
  - Going fast like all other times

## Teleporting

- **General**
  - Undo/Redo teleport, a stack
    - When teleporting to a player, remember both their position and which player it was, so when revisiting that entry in the undo stack it teleports you to where the player is at, not where they were. Same for locations if there is any moving locations
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
    - Maybe runtime placeable locations. Could also be tied into GM proxies. Uncertain

## Spawning GM Proxies

## Spawning Items

## Summoning Players

- Selecting players
  - Try point and click selection of players
  - Show count of how many players are selected
  - Option to sort players by selected state
  - Closing the UI does not clear selection
  - Potentially save and load selections
  - Select all
  - Select none
  - Invert selection
- Summoning multiple players spreads them out
  - In a circle around the summoner (must have)
  - Maybe an alternative which spreads them out all facing the same direction as the summoner
- Visual indictor on the ground where somebody is about to be summoned, local only to the summoner

## Discretely Talking With Players

- Also using the player selection system
- While active, only selected players will be able to hear you
- The voice range remains unchanged

# Relevant Differences To Existing Systems

- No thumbnails for players
- No watch feature
  - Hardly useful in general, especially with undo teleport
- No teleporting back to where you disconnected from
  - An out of character toggle on avatars or invisible avatars plus teleporting to players and locations solves this issue ultimately more elegantly
