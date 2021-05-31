#!/usr/bin/env bash

# Copyright (c) 2020-2021 Eli Aloni a.k.a elix22.
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.
#

export PROJECT_UUID='com.elix22.admob'
export PROJECT_NAME='Admob' 
export JAVA_PACKAGE_PATH='java/com/elix22/admob' 

# Add the plugins that you want to include 
# The plugins must reside in the Plugins folder and the plugin name must match the plugin folder name , the syntax is :
# export PLUGINS=('plugin-1' 'plugin-2' 'plugin-3' ... )
export PLUGINS=('AdmobPlugin')

# Gooogle Mobile Ads Application ID , below id is a test id , set your own App ID for your game/app
# More information can be found in :
# https://developers.google.com/admob/android/test-ads
# https://developers.google.com/admob/ios/test-ads
export GAD_APPLICATION_ID='ca-app-pub-3940256099942544~1458002511'


# Add Android permissions
# export ANDROID_PERMISSIONS=('permission 1' 'permission 2' 'permission 3' ... )
export ANDROID_PERMISSIONS=('android.permission.INTERNET' 'android.permission.ACCESS_NETWORK_STATE')

# Add dependcies for Android
# export ANDROID_DEPENDENCIES=('dependency 1' 'dependency 2' 'dependency 3' ... )
export ANDROID_DEPENDENCIES=('com.google.android.gms:play-services-ads:20.1.0')

# Uncomment if you want to Specify specific Android NDK  version
# export ANDROID_NDK_VERSION='21.0.6113669'



