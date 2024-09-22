# Nebula: User Guide and Minimal Sample Scene Description

## Introduction

Welcome to **Nebula**, a modular music visualization system for Unity. This guide will walk you through the steps to set up Nebula in your project, create a minimal sample scene, and explain how the components interact to produce music-reactive visualizations.

## Prerequisites

- **Unity Version:** Unity 2021.1 or newer
- **Basic Knowledge:** Familiarity with Unity's interface, GameObjects, and basic C# scripting

## Getting Started

### 1. Installation

#### Option 1: Import the Package

1. **Download Nebula:**
   - Obtain the Nebula package (`Nebula.unitypackage`) from the repository or distribution source.

2. **Import into Unity:**
   - Open your Unity project.
   - Go to **Assets** > **Import Package** > **Custom Package...**
   - Navigate to the `Nebula.unitypackage` file and select it.
   - In the Import dialog, ensure all files are selected and click **Import**.

#### Option 2: Copy the Folder

1. **Copy Nebula Folder:**
   - Copy the `Nebula` folder into your project's `Assets` directory.

2. **Refresh Unity:**
   - Unity will automatically detect the new files and compile the scripts.

### 2. Setting Up the Music Processor

1. **Create a GameObject:**
   - In the **Hierarchy** window, right-click and select **Create Empty**.
   - Rename the GameObject to `NebulaProcessor`.

2. **Attach MusicProcessor Script:**
   - With `NebulaProcessor` selected, click **Add Component** in the Inspector.
   - Search for `MusicProcessor` and add it.

3. **Add an Audio Source:**
   - Still on `NebulaProcessor`, click **Add Component** > **Audio** > **Audio Source**.
   - Assign your music clip to the **AudioClip** field.
   - Ensure **Play On Awake** is checked if you want the music to start automatically.

4. **Configure MusicProcessor:**
   - In the `MusicProcessor` component:
     - Assign the **Audio Source** you just added.
     - Choose the **Processing Mode** (`Live` or `Preprocessed`).

### 3. Creating a Minimal Sample Scene

#### Step 1: Set Up the Scene

1. **Save the Scene:**
   - Go to **File** > **Save As...** and name your scene (e.g., `MinimalSampleScene`).

2. **Add Lighting and Camera (if not present):**
   - Ensure your scene has a **Directional Light** and a **Main Camera**.

#### Step 2: Add a Visual Element

1. **Create a Cube:**
   - In the **Hierarchy**, right-click and select **3D Object** > **Cube**.
   - Rename it to `ReactiveCube`.

2. **Position the Cube:**
   - Set the **Position** of `ReactiveCube` to `(0, 0, 0)`.

3. **Attach a Visual Element Script:**
   - With `ReactiveCube` selected, click **Add Component**.
   - Search for `ScaleWithMusic` and add it.

4. **Configure the Visual Element:**
   - In the `ScaleWithMusic` component:
     - Set **Sensitivity** to adjust how much the cube scales.
     - Optionally, specify frequency bands in the **Frequency Bands** array.

#### Step 3: Test the Scene

1. **Save Your Work:**
   - Go to **File** > **Save**.

2. **Play the Scene:**
   - Click the **Play** button.
   - Observe the cube scaling in response to the music.

## Description of the Minimal Sample Scene

In this minimal sample scene, we have:

- **NebulaProcessor GameObject:**
  - Contains the `MusicProcessor` and an `AudioSource` with your music clip.
  - Analyzes the music and provides data to visual elements.

- **ReactiveCube GameObject:**
  - A cube at the center of the scene.
  - Has the `ScaleWithMusic` script attached, which scales the cube based on the music's amplitude.

- **Main Camera and Lighting:**
  - Default Unity components to visualize the scene.

### How Components Interact

- **MusicProcessor:**
  - Processes the audio in real-time (or uses preprocessed data).
  - Extracts audio features like amplitude and frequency bands.
  - Provides this data to visual elements via methods and events.

- **ScaleWithMusic Script:**
  - Inherits from `AudioReactiveElement`.
  - Subscribes to updates from `MusicProcessor`.
  - Adjusts the scale of the cube based on the amplitude of the music.

- **Visual Outcome:**
  - The cube scales up and down in sync with the music's dynamics.
  - Provides a visual representation of the music's energy.

## Using Unity's Timeline (Optional)

To control visual elements over time using Unity's Timeline:

1. **Open the Timeline Window:**
   - Go to **Window** > **Sequencing** > **Timeline**.

2. **Create a Timeline Asset:**
   - With `ReactiveCube` selected, click **Create** in the Timeline window.
   - Save the Timeline asset (e.g., `ReactiveCubeTimeline`).

3. **Add an AudioReactiveTrack:**
   - In the Timeline window, click **Add** (`+`) and select **AudioReactiveTrack**.

4. **Add an AudioReactiveClip:**
   - Right-click on the track area and select **Add Clip** > **Audio Reactive Clip**.

5. **Configure the Clip:**
   - Select the clip and adjust its duration and parameters.
   - Assign the `ReactiveCube` to the **Track Binding** field.

6. **Adjust Parameters Over Time:**
   - Use keyframes and curves to modify properties like **Sensitivity** during playback.

## Creating Custom Visual Elements

To create your own visual element that reacts to music:

1. **Create a New Script:**
   - In the **Assets/Nebula/Core/VisualElements/** folder, right-click and select **Create** > **C# Script**.
   - Name it (e.g., `ColorChangeWithMusic.cs`).

2. **Edit the Script:**

   ```csharp
   using UnityEngine;

   public class ColorChangeWithMusic : AudioReactiveElement
   {
       private Renderer objRenderer;

       protected override void Start()
       {
           base.Start();
           objRenderer = GetComponent<Renderer>();
       }

       protected override void ReactToMusic()
       {
           float amplitude = MusicProcessor.Instance.GetAmplitude();
           Color color = Color.Lerp(Color.blue, Color.red, amplitude * sensitivity);
           objRenderer.material.color = color;
       }
   }
   ```

3. **Attach to a GameObject:**
   - Create a new GameObject (e.g., a sphere).
   - Attach the `ColorChangeWithMusic` script.
   - Ensure the object has a **Renderer** component.

4. **Test the Custom Element:**
   - Play the scene and observe the color changes in response to the music.

## Preprocessing Audio Data (Advanced)

Using preprocessed data can improve performance:

1. **Add the AudioPreprocessor:**
   - Create a new GameObject (e.g., `AudioPreprocessor`).
   - Attach the `AudioPreprocessor` script.

2. **Configure the Preprocessor:**
   - In the `AudioPreprocessor` component:
     - Assign the audio clip to preprocess.
     - Set parameters like sample size and frequency bands.

3. **Run the Preprocessor:**
   - Use the provided method or editor function to preprocess the audio.
   - This will generate a `PreprocessedAudioData.asset` in the **Resources** folder.

4. **Set MusicProcessor to Preprocessed Mode:**
   - In the `MusicProcessor` component:
     - Change **Processing Mode** to `Preprocessed`.
     - Assign the `PreprocessedAudioData.asset`.

5. **Test the Scene:**
   - Play the scene to ensure visual elements react using preprocessed data.

## Tips and Troubleshooting

- **No Reaction from Visual Elements:**
  - Ensure `MusicProcessor` is properly initialized and the Audio Source is playing.
  - Check that the visual element scripts are correctly attached and enabled.

- **Audio Not Playing:**
  - Confirm that the audio clip is assigned and **Play On Awake** is checked.
  - Verify your system's audio settings.

- **Performance Issues:**
  - Use **Preprocessed** mode for better performance.
  - Optimize visual elements and reduce complexity if necessary.

- **Adjusting Sensitivity:**
  - Tweak the **Sensitivity** parameter in visual element scripts to achieve desired reactions.

## Conclusion

By following this guide, you've set up a minimal sample scene using Nebula to create music-reactive visualizations. You can extend this setup by adding more visual elements, customizing behaviors, and integrating Unity's Timeline for advanced control.

Explore and experiment to create unique and dynamic visual experiences synchronized with music!

## Additional Resources

- **Nebula Documentation:**
  - Located in `Assets/Nebula/Documentation/README.md`.

- **Unity Tutorials:**
  - [Unity Learn](https://learn.unity.com/) for in-depth tutorials on scripting, Timeline, and more.

- **Community Support:**
  - Join Unity forums or Nebula's support channels for assistance and to share your creations.

---
