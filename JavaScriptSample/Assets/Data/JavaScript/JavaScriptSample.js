var System = importNamespace('System');
var Urho = importNamespace('Urho');
var IO = importNamespace('Urho.IO');


// some shorthand variable declarations , to make life easy
var Log = IO.Log;
var Input = Application.Input;
var ResourceCache = Application.ResourceCache;
var UI = Application.UI;
var Platforms = Urho.Platforms;
var Graphics = Application.Graphics;
var Key = Urho.Key;
var Vector3 = Urho.Vector3;
var Quaternion = Urho.Quaternion;
var StringHash = Urho.StringHash;
var Renderer = Application.Renderer;
var MathHelper = Urho.MathHelper;
var LightType = Urho.LightType;
var TouchState = Urho.TouchState;



// Global vars
var Yaw = 0.0;
var Pitch = 0.0;
var CameraNode = null

function CreateScene() {

    var scene = new Urho.Scene();
    scene.CreateComponent(new StringHash("Octree"));

    var planeNode = scene.CreateChild("Plane");
    planeNode.Scale = new Vector3(100, 1, 100);
    var planeObject = planeNode.CreateComponent(new StringHash("StaticModel"));
    planeObject.Model = ResourceCache.GetModel("Models/Plane.mdl");
    planeObject.SetMaterial(ResourceCache.GetMaterial("Materials/StoneTiled.xml"));


    var lightNode = scene.CreateChild("DirectionalLight");
    lightNode.SetDirection(new Vector3(0.6, -1.0, 0.8)); // The direction vector does not need to be normalized
    var light = lightNode.CreateComponent(new StringHash("Light"));
    light.LightType = LightType.Directional;

    var skyNode = scene.CreateChild("Sky");
    skyNode.SetScale(500.0); // The scale actually does not matter
    var skybox = skyNode.CreateComponent(new StringHash("Skybox"));
    skybox.Model = ResourceCache.GetModel("Models/Box.mdl");
    skybox.SetMaterial(ResourceCache.GetMaterial("Materials/Skybox.xml"));

    var rand = new System.Random();
    for (var i = 0; i < 200; i++) {
        var mushroom = scene.CreateChild("Mushroom");
        mushroom.Position = new Vector3(rand.Next(90) - 45, 0, rand.Next(90) - 45);
        mushroom.Rotation = new Urho.Quaternion(0, rand.Next(360), 0);
        mushroom.SetScale(0.5 + rand.Next(20000) / 10000.0);
        var mushroomObject = mushroom.CreateComponent(new StringHash("StaticModel"));
        mushroomObject.Model = ResourceCache.GetModel("Models/Mushroom.mdl");
        mushroomObject.SetMaterial(ResourceCache.GetMaterial("Materials/Mushroom.xml"));
    }

    CameraNode = scene.CreateChild("camera");
    var camera = CameraNode.CreateComponent(new StringHash("Camera"));

    // Set an initial position for the camera scene node above the plane
    CameraNode.Position = new Vector3(0, 5, 0);

    Renderer.SetViewport(0, new Urho.Viewport(Application.Context, scene, camera, null));

    Log.Info("Scene created in JavaScript");

}


function OnUpdate(timeStep) {
    // Log.Info("OnUpdate " + timeStep);

    if (Platform == Platforms.Android ||
        Platform == Platforms.iOS) {
        MoveCameraByTouches(timeStep);
    }

    SimpleMoveCamera3D(timeStep);

}


function SimpleMoveCamera3D(timeStep, moveSpeed = 10.0) {
    const mouseSensitivity = 0.1;

    if (UI.FocusElement != null)
        return;

    var mouseMove = Input.MouseMove;
    Yaw += mouseSensitivity * mouseMove.X;
    Pitch += mouseSensitivity * mouseMove.Y;
    Pitch = MathHelper.Clamp(Pitch, -90, 90);


    CameraNode.Rotation = new Quaternion(Pitch, Yaw, 0);

    var movement = moveSpeed * timeStep;
    if (Input.GetKeyDown(Key.W)) CameraNode.Translate(new Vector3(0, 0, movement));
    if (Input.GetKeyDown(Key.S)) CameraNode.Translate(new Vector3(0, 0, -movement));
    if (Input.GetKeyDown(Key.A)) CameraNode.Translate(new Vector3(-movement, 0, 0));
    if (Input.GetKeyDown(Key.D)) CameraNode.Translate(new Vector3(movement, 0, 0));

}

function MoveCameraByTouches(timeStep) {

    const TouchSensitivity = 2.0;

    for (var i = 0, num = Input.NumTouches; i < num; ++i) {
        var state = Input.GetTouch(i);
        if (state.TouchedElement != null)
            continue;

        if (state.Delta.X != 0 || state.Delta.Y != 0) {
            var camera = CameraNode.GetComponent(new StringHash("Camera"));
            Yaw += TouchSensitivity * camera.Fov / Graphics.Height * state.Delta.X;
            Pitch += TouchSensitivity * camera.Fov / Graphics.Height * state.Delta.Y;
            CameraNode.Rotation = new Quaternion(Pitch, Yaw, 0);
        }
    }
}