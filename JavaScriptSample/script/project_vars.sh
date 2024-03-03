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

export PROJECT_UUID='com.elix22.javascriptsample'
export PROJECT_NAME='JavaScriptSample' 
export JAVA_PACKAGE_PATH='java/com/elix22/javascriptsample' 

# add external reference dll's that should be part of the build for iOS and Android , the dll's must be present in the References folder
export DOTNET_REFERENCE_DLL=('Jint.dll' 'Esprima.dll')

export ANDROID_ARCHITECTURE=('arm64-v8a' 'armeabi-v7a' 'x86' 'x86_64')
# either portrait or landscape , relevant only to mobile platforms such as Android or iOS.
export SCREEN_ORIENTATION='landscape'