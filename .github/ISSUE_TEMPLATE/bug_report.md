name: Bugreport
description: Report a bug related to the Food Shelves mod here.
labels: ["status: new"]

body:
  - type: input
    id: title
    attributes:
      label: Title
      description: A short title describing the bug
      placeholder: Example: Fruit Basket doesn't drop contents when broken
    validations:
      required: true

  - type: input
    id: gameversion
    attributes:
      label: Game Version
      description: What Vintage Story game version are you using?
      placeholder: v1.20.10
    validations:
      required: false

  - type: input
    id: modversion
    attributes:
      label: Food Shelves Mod Version
      description: Which version of the Food Shelves mod are you using?
      placeholder: v2.1.3
    validations:
      required: true

  - type: dropdown
    id: onlyfoodshelves
    attributes:
      label: Does this issue happen when only Food Shelves is enabled (no other mods)?
      description: Disable all other mods and test again, if possible.
      options:
        - Yes
        - No
        - I don't know
    validations:
      required: false

  - type: textarea
    id: description
    attributes:
      label: Description
      description: Explain the issue you're running into.
      placeholder: What exactly goes wrong? When did it start happening? What are the steps to reproduce?
    validations:
      required: false

  - type: input
    id: labels
    attributes:
      label: Labels
      description: Suggested tags for this issue (e.g., bug, visual bug, crash, performance)
      placeholder: bug, visual bug
    validations:
      required: false
