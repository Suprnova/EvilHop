# EvilHop Testing

## Expected File Structure

``/artifacts/{game}/{build}/{platform}/{region}/{language}/``

where:

* **{game}** is the shorthand identifier for the game.
    - *n100f* - Scooby-Doo! Night of 100 Frights
    - *bfbb* - SpongeBob SquarePants: Battle for Bikini Bottom
    - *tssm* - The SpongeBob SquarePants Movie
    - *incredibles* - The Incredibles
    - *rotu* - The Incredibles: Rise of the Underminer
    - *rat* - Ratatouille (Heavy Iron Studios prototype)
* **{build}** is the build identifier of the game.
    - *release* - the official consumer release
        - optionally suffixed by ``_r{n}``, representing the *nth* revision.
    - *prototype_``YYYY-MM-DD``* - a prototype built on the specified date in ISO 8601 formatting.
* **{platform}** is the shorthand identifier for the platform.
    - *GC* - Nintendo GameCube
    - *PC* - Microsoft Windows
    - *P2* - PlayStation 2
    - *XB* - Xbox
* **{region}** is the region of the game.
    - *NTSC-U* - North America
    - *PAL* - Europe
    - *NTSC-J* - Japan
* **{language}** is the language of the game. For games with multiple languages, they are sorted alphabetically and joined by hyphens (``-``).
    - *DE* - German
    - *FR* - French
    - *JP* - Japanese
    - *NL* - Dutch
    - *UK* - British English
    - *US* - American English

For example, the path ``/artifacts/rotu/prototype_2005-09-15/GC/PAL/FR-UK`` conveys the following information to the testing manager:

* Game: The Incredibles: Rise of the Underminer
* Build: Prototype, built on September 15th, 2005.
* Platform: Nintendo GameCube
* Region: Europe (PAL)
* Supported Languages: French, British English
