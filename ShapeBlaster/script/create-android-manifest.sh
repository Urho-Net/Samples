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

. script/project_vars.sh

unamestr=$(uname)
CWD=$(pwd)
DESTINATION="Android/app/src/main"


rm ${DESTINATION}/AndroidManifest.xml
touch ${DESTINATION}/AndroidManifest.xml

echo ${DESTINATION}/AndroidManifest.xml

echo "<?xml version=\"1.0\" encoding=\"utf-8\"?>" >> ${DESTINATION}/AndroidManifest.xml
echo "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"${PROJECT_UUID}\">" >> ${DESTINATION}/AndroidManifest.xml

if [ -n "$ANDROID_PERMISSIONS" ] ; then
    for i in "${ANDROID_PERMISSIONS[@]}"
        do
            echo "  <uses-permission android:name=\"${i}\" />" >> ${DESTINATION}/AndroidManifest.xml
        done
fi

echo "  <application android:allowBackup=\"true\" android:icon=\"@mipmap/ic_launcher\" android:label=\"@string/app_name\" android:roundIcon=\"@mipmap/ic_launcher_round\" android:supportsRtl=\"true\" android:theme=\"@style/AppTheme\">" >> ${DESTINATION}/AndroidManifest.xml

if [ -n "$GAD_APPLICATION_ID" ] ; then
    echo "      <meta-data android:name=\"com.google.android.gms.ads.APPLICATION_ID\" android:value=\"${GAD_APPLICATION_ID}\"/>" >> ${DESTINATION}/AndroidManifest.xml
fi

echo "      <activity android:name=\".MainActivity\">" >> ${DESTINATION}/AndroidManifest.xml
echo "          <intent-filter>"  >> ${DESTINATION}/AndroidManifest.xml
echo "              <action android:name=\"android.intent.action.MAIN\" />"  >> ${DESTINATION}/AndroidManifest.xml
echo "              <category android:name=\"android.intent.category.LAUNCHER\" />"  >> ${DESTINATION}/AndroidManifest.xml
echo "          </intent-filter>"  >> ${DESTINATION}/AndroidManifest.xml
echo "      </activity>" >> ${DESTINATION}/AndroidManifest.xml

echo "      <activity android:name=\".UrhoMainActivity\" android:configChanges=\"keyboardHidden|orientation|screenSize\" android:screenOrientation=\"landscape\" android:theme=\"@android:style/Theme.NoTitleBar.Fullscreen\"/>"  >> ${DESTINATION}/AndroidManifest.xml

echo "  </application>" >> ${DESTINATION}/AndroidManifest.xml
echo "</manifest>" >> ${DESTINATION}/AndroidManifest.xml