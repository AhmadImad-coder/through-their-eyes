# Through Their Eyes

Unity WebGL senior project: an immersive contamination OCD simulation set in a coffee shop.

## Local Unity Project

Open this folder in Unity 6.3 LTS:

```text
/Users/ahmadimad/Desktop/current courses/Senior Project/game
```

The main scene is:

```text
Assets/Scenes/SampleScene.unity
```

## WebGL Build For Render

Render hosts the committed static WebGL output from:

```text
RenderSite/
```

To rebuild it locally from Unity:

```bash
/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath "/Users/ahmadimad/Desktop/current courses/Senior Project/game" \
  -buildTarget WebGL \
  -executeMethod WebGLBuildScript.Build \
  -logFile /tmp/through-their-eyes-webgl-build.log
```

The `render.yaml` file configures Render as a static site and publishes `RenderSite/`.
