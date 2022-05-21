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

export PROJECT_UUID='com.elix22.samplygame'
export PROJECT_NAME='SamplyGame' 
export JAVA_PACKAGE_PATH='java/com/elix22/samplygame' 

# Supported Android architectures  , remove any architecture that is not needed inorder to redcue apk size.
# The minimal ABI on ARM devices is armeabi-v7a , that's the minimal one  to make is work on ARM based devices
# In case of an Intel based processor (x86/x86_64) , one must add 'x86' or and 'x86_64' to mkae it work on such device
# export ANDROID_ARCHITECTURE=('arm64-v8a' 'armeabi-v7a' 'x86' 'x86_64')
export ANDROID_ARCHITECTURE=('arm64-v8a' 'armeabi-v7a')