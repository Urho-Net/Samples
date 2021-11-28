# FeatureSamples
This directory contains the various samples that showcase \
The features of the C# multiplatform game development framework
[Urho.Net](https://github.com/Urho-Net/Urho.Net).\
The sample run on all supported platforms , macOS,Windows,Linux,iOS,Android and Web browsers .\
Please follow the installation instructions of [Urho.Net](https://github.com/Urho-Net/Urho.Net)\
Make sure you are always using the latest version of [Urho.Net](https://github.com/Urho-Net/Urho.Net) , otherwise some of the samples might not work.

# Google Play Store  
[FeatureSamples APK](https://play.google.com/store/apps/details?id=com.elix22.urhonetsamples)



# Running the Feature Samples on Desktop.
Make sure that you followed the instructions and installed all the rquired dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net).\
This applies to Windows,Linux and MacOS.\

git clone https://github.com/Urho-Net/Samples.git
* cd Samples
* code FeatureSamples
* Visual Studio Code will open the project.
* Run & Debug (Ctrl+Shift+D) , press play


# Running the samples on a real Android device
Make sure that you followed the instructions and installed all the rquired dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net)\
Only one Android device should be connected to your desktop/laptop.\
Support of multiple connected devices will be added in the future.
* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Tasks:Run Task , pick android-deploy-debug
* The sample will compile , will be installed and run on the Android device.




# Running the samples on a real iOS device 
Make sure that you followed the instructions and installed all the rquired dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net)
* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Tasks:Run Task , pick ios-deploy
* Enter your developer ID
* The sample will compile , will be installed and run on the iOS device.

# Running the samples on a Web browser
  Make sure that you followed the instructions and installed all the rquired dependencies as described in [Urho.Net](https://github.com/Urho-Net/Urho.Net)

* From VS Code , Ctrl+Shift+P (Cmd+Shift+P on Mac)
* Choose Tasks: Run Task
* Choose web-build
* The build will generate Web folder in the project directory
* The Web folder contains everything that is needed for web deployment.
* You can test it on your local browser with the [Live Server extention](https://marketplace.visualstudio.com/items?itemName=ritwickdey.LiveServer)



