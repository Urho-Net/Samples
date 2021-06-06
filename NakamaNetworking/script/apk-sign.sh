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

while getopts b:d:g:k:i:o:n: option
do
case "${option}"
in
b) BUILD=${OPTARG};;
d) DEPLOY=${OPTARG};;
g) GENERATE_KEY=${OPTARG};;
k) KEY_STORE=${OPTARG};;
i) APK_INPUT_PATH=${OPTARG};;
o) APK_OUTPUT_PATH=${OPTARG};;
n) APK_NAME=${OPTARG};;
esac
done

KEY_STORE=$(echo "$KEY_STORE" | tr -d ' ')
if [[ "${KEY_STORE}" == "." ]]; then 
    KEY_STORE=""
fi

CWD=$(pwd)
KEY_STORE_PATH_INFO=${CWD}/keystore_path.txt

 path_to_executable=$(which zipalign)
if [ -x "$path_to_executable" ]; then
    echo "found zipalign: $path_to_executable"
else
    echo "No zipalign in path. usually this can be found in ANDROID-SDK/build-tools/[....]/zipalign"
    exit 1
fi



path_to_executable=$(which apksigner)
if [ -x "$path_to_executable" ]; then
    echo "found apksigner: $path_to_executable"
    APK_SIGNER=apksigner
else
    echo "No apksigner in path. usually this can be found in ANDROID-SDK/build-tools/[....]/apksigner"
    path_to_executable=$(which jarsigner)
    if [ -x "$path_to_executable" ]; then
        echo "found jarsigner: $path_to_executable"
        JAR_SIGNER=jarsigner
    else
        echo "No jarsigner in path. usually this can be found in JDK/bin/jarsigner"
        exit 1
    fi
fi

generate_keystore()
{
    path_to_executable=$(which keytool)
    if [ -x "$path_to_executable" ]; then
        echo "found keytool: $path_to_executable"
    else
        echo "No keytool in path. usually this can be found in /usr/bin/keytool"
        exit 1
    fi  
    echo "Enter path for output generated keystore."
    read -p "keystore path: " key_store_path
    if [[ "$key_store_path" == "" ]]; then
        echo
        echo "keystore path not specified , will generate in the current directory"
        key_store_path=$(pwd)
    fi
    
    mkdir -p ${key_store_path}
    cd ${key_store_path}
    rm -f android-release-key.jks
    keytool -genkey -v -storepass Android -keypass Android -keystore android-release-key.jks -keyalg RSA -keysize 2048 -validity 10000 -alias my-alias
    KEY_STORE=$(pwd)/android-release-key.jks
    cd ${CWD}


}

check_keystore_path_info()
{
    if [ -f "${KEY_STORE_PATH_INFO}" ]; then 
        TMP_KEY_STORE=$(cat "${KEY_STORE_PATH_INFO}")
        if [ -f "${TMP_KEY_STORE}" ]; then
            if [ ! -d "$TMP_KEY_STORE" ]; then
                echo "keystore taken from ${KEY_STORE_PATH_INFO} , keystore file is ${TMP_KEY_STORE}"
                KEY_STORE="${TMP_KEY_STORE}"
            fi
        fi
    fi
}

set_relative_to_full_path()
{
    echo $(echo "$(cd "$(dirname "$1")"; pwd)/$(basename "$1")")
}

if [[ "$GENERATE_KEY" == "1" ]]; then
    generate_keystore
fi


if [[ "$APK_NAME" == "" ]]; then
    echo "apk name was not provided , using default app-release-unsigned.apk."
    APK_NAME="app-release-unsigned.apk"
fi

if [[ "$APK_INPUT_PATH" == "" ]]; then
    echo "apk source path was not specified , using current directory"
    APK_INPUT_PATH=${CWD}
fi

if [[ "$APK_OUTPUT_PATH" == "" ]]; then
    echo "apk destination path was not specified , using current directory"
    APK_OUTPUT_PATH=${CWD}
fi

if [[ "$KEY_STORE" == "" || "$KEY_STORE" == " " ]]; then 
    check_keystore_path_info
    if [ ! -f "${KEY_STORE}" ]; then
        KEY_STORE=${CWD}/android-release-key.jks
        echo "searching keystore in ${KEY_STORE}"
        if [ -f "${KEY_STORE}" ]; then
            echo "keystore path was not specified , using ${KEY_STORE}"
        else 
            echo "keystore was not found generate"
            generate_keystore
        fi
    fi
fi


if [ ! -f "${KEY_STORE}" ]; then
    check_keystore_path_info
    if [ ! -f ${KEY_STORE} ]; then
        echo "keystore was not found generate"
        generate_keystore
    fi
fi

echo "KEY_STORE=${KEY_STORE}"

# always create a new KEY_STORE_PATH_INFO , overwrite older path
if [ -f ${KEY_STORE} ]; then 
    KEY_STORE=$(set_relative_to_full_path ${KEY_STORE})
    rm -f ${KEY_STORE_PATH_INFO}
    touch ${KEY_STORE_PATH_INFO}
    echo ${KEY_STORE} > ${KEY_STORE_PATH_INFO}
fi

filename=$(basename -- "${APK_INPUT_PATH}/${APK_NAME}" .apk)
filename_with_ext=$(basename -- "${APK_INPUT_PATH}/${APK_NAME}")

zipalign -f -p 4 ${APK_INPUT_PATH}/${APK_NAME} ${APK_OUTPUT_PATH}/${filename}-aligned.apk

out_file_name=$(echo ${filename} | sed 's/unsigned//g' | sed 's/--/-/g' | sed 's/\-\././g')
out_file_name=${out_file_name}-signed.apk
out_file_name=$(echo ${out_file_name} |  sed 's/--/-/g' | sed 's/\-\././g')
rm -f ${APK_OUTPUT_PATH}/${out_file_name}


if [ -n "$APK_SIGNER" ]; then
    ${APK_SIGNER} sign --ks ${KEY_STORE} --ks-pass pass:Android --out ${APK_OUTPUT_PATH}/${out_file_name} ${APK_OUTPUT_PATH}/${filename}-aligned.apk
elif [ -n "$JAR_SIGNER" ]; then
    ${JAR_SIGNER}  -keystore ${KEY_STORE} -storepass Android ${APK_OUTPUT_PATH}/${filename}-aligned.apk -signedjar ${APK_OUTPUT_PATH}/${out_file_name} my-alias
fi

rm ${APK_OUTPUT_PATH}/${filename}-aligned.apk
