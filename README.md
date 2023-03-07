# Samples
This directory contains the various samples that showcase \
The features of the C# multiplatform game development framework
[Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-6.x).\
Please follow the installation instructions of [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-6.x)\
## Check the Wiki for quick start
[Wiki](https://github.com/Urho-Net/Samples/wiki)

## Make sure you are always using the latest version of [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-6.x) , otherwise some of the samples might not work.
## If you update Urho.Net , you will also have to update old projects.
In order to do that you have to run :\
From Urho-Net folder **./update-project.sh project-path**\
On Windows it's **update-project.bat project-path**\

# Live Web Browser samples 
 [Urho.Net Live Web Browser Feature Samples](https://elix22.itch.io/urhonet-feature-samples)

# Google Play  
[Urho.Net Feature Samples](https://play.google.com/store/apps/details?id=com.elix22.urhonetsamples)


# Running the Feature Samples on Desktop.
Make sure that you followed the instructions and installed all the required dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-6.x).\
This applies to Windows,Linux and MacOS.\

git clone https://github.com/Urho-Net/Samples.git
* cd Samples
* code FeatureSamples
* Visual Studio Code will open the project.
* press F5 to run


# Running the samples on a real Android device
Make sure that you followed the instructions and installed all the required dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-6.x)\
Only one Android device should be connected to your desktop/laptop.\
Support of multiple connected devices will be added in the future.
* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Tasks:Run Task , pick android-deploy-debug
* The sample will compile , will be installed and run on the Android device.

# Running the samples on a real iOS device 
Make sure that you followed the instructions and installed all the required dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-6.x)
* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Tasks:Run Task , pick ios-deploy
* Enter your developer ID
* The sample will compile , will be installed and run on the iOS device.

# Running the samples on a Web browser
  Make sure that you followed the instructions and installed all the rquired dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net/tree/dotnet-6.x)

* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Choose Tasks: Run Task
* Choose web-build
* The build will generate Web folder in the project directory
* The Web folder contains everything that is needed for web deployment.
* You can test it on your local browser with the [Live Server extention](https://marketplace.visualstudio.com/items?itemName=ritwickdey.LiveServer)






