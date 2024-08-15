# Manual

[![Manual Video](docs/cover.png)](https://www.youtube.com/watch?v=3NynPgEyLNA)

Manual is an advanced ComfyUI Frontend. I developed it from scratch to be compatible with any AI that will be created in the future (and it's still a work in progress). I've written 100,000 lines of code for this project.

## Platforms

-   Windows only (for now).

## Installation

-   Download the [**Manual_Portable.zip**](https://github.com/yowipr/Manual/releases/download/1.0.0-beta/Manual_Portable.zip).
-   or in Civitai: https://civitai.com/models/644188/manual
-   You need **ComfyUI-Manual custom node** in ComfyUI: [ComfyUI-Manual](https://github.com/yowipr/ComfyUI-Manual).
-   Copy and paste the ComfyUI folder path into Manual by navigating to Editor -> Preferences.
-   In the bottom right corner, click the circle, choose Open Local Server, and wait until the server starts (the circle will turn blue).
-   In the Latent Nodes Editor, click View -> Refresh to load your nodes and models.
-   Select the models and LoRa you have downloaded and wish to use.
-   Write a prompt, click generate, and you're done. Enjoy all the tools!

Alternative use: Is not necesary need to open the server inside Manual, you can open ComfyUI server externally and use Manual

# Main Features

### User Interface
Designed to be friendly to traditional artists (now digital artists are traditionals? 🤔)

[Watch the full video on YouTube](https://www.youtube.com/watch?v=3NynPgEyLNA)

<img src="docs/sketch.gif" alt="User Interface">

### Multiple Workflows and Templates
Manual has Templates for workflow nodes and Prompt Styles.

Add, Duplicate, Save, Load

you can drop a .json file with your workflows made with ComfyUI, but for some reason you can't do it from Manual to ComfyUI, who knows

for exporting: File -> Export -> Export Prompt Preset (PromptPreset is a workflow node)

![Multiple Workflows](docs/multiple_workflows.gif)

### Prompt Styles
All workflow nodes are linked to a Prompt Style, even this can be deactivated in every workflow node and have their own Prompt Style.
Prompt Styles are useful for testing different text prompts or making characters

<img src="docs/prompt_styles.gif" alt="Multiple Workflows">

### Drivers
With Drivers you can link the fields to the Prompt Style or provide a lambda expression.

**Attach a Prompt Style to a workflow node automatically**: in the nav bar of Latent Nodes Editor -> PromptPreset -> Automatic Drivers

**Custom Drivers setup**: right click in a node field -> Edit Driver -> write Prompt Style property name (usually Manual recommends a name)

for an advanced driver, you can use "source." followed by the property name.

- example 1: Strength
- example 2: source.Strength / 2
- example 3: source.Strength + source.CFG

<img src="docs/drivers.gif" alt="Drivers" width="400">


### Manual Nodes
- Layer Node
- Output Node

 <img src="docs/manual_nodes.gif" alt="Manual Nodes" width="400">

### More
Manual might contain things I don't even remember, a lot of easter eggs lol.

 <img src="docs/bypass_lora.gif" alt="More" width="400">

## Considerations

-   To make all the tools work, you'll need some additional custom nodes from ComfyUI:
    -   [ComfyUI Impact Pack](https://github.com/ltdrdata/ComfyUI-Impact-Pack)
    -   [ComfyUI Essentials](https://github.com/cubiq/ComfyUI_essentials)
    -   [ComfyUI ControlNet Aux](https://github.com/Fannovel16/comfyui_controlnet_aux)
-   The tools in Manual by default work with SDXL; however, you can modify the Templates in the folder Manual/Resources/Templates/PromptPresets
-   Manual projects do not save generation history (the editor on the right); always place your images in layers.
-   Be CAREFUL with Save Projects, Manual is still in beta.

## Licence

-   Free. Commercial use allowed. Enjoy it!
-   I am not responsible for what you generate with Manual.
-   Period.

## What's Next

-   The software is in beta, so feedback is appreciated.
-   For suggestions or assistance, you can reach me on [**Discord**](https://discord.gg/msKBTgu8Ca).
-   Contributions are welcome if you're a programmer or have ideas.

## Goals

-   Make AI accessible for everyone.
-   Establish AI as a standard tool for professional and monetizable artwork; like films, anime, etc.

:sparkles: _Made with love._ :sparkles:
