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
if [[ "$OSTYPE" == "darwin"* ]]; then

    . script/project_vars.sh

    verify_dir_exist_or_exit()
    {
        if [ ! -d $1 ] ; then
            echo "$1 not found , are you sure that URHONET_HOME_ROOT=${URHONET_HOME_ROOT} is pointing to the right place ? "
            exit 1
        fi
    }

    if [ ! -f ~/.urhonet_config/urhonethome ]; then
        echo  "1 Urho.Net is not configured , please  run configure.sh (configure.bat on Windows) from the Urho.Net installation folder  "
        exit -1
    fi

    URHONET_HOME_ROOT=$(cat ~/.urhonet_config/urhonethome)

    if [ ! -d "$URHONET_HOME_ROOT" ]; then
        echo  "Urho.Net is not configured , please  run configure.sh (configure.bat on Windows) from the Urho.Net installation folder  "
        exit -1
    else
        echo "URHONET_HOME_ROOT=${URHONET_HOME_ROOT}"

        if [ ! -d libs/dotnet/bcl/ios ] ; then
            verify_dir_exist_or_exit "${URHONET_HOME_ROOT}/template/libs/dotnet/bcl/ios" 
            mkdir -p libs/dotnet/bcl/ios
            cp "-r"  ${URHONET_HOME_ROOT}/template/libs/dotnet/bcl/ios/*  libs/dotnet/bcl/ios/
        fi

        if [ ! -d libs/dotnet/urho//mobile/ios ] ; then
            verify_dir_exist_or_exit "${URHONET_HOME_ROOT}/template/libs/dotnet/urho//mobile/ios"
            mkdir -p libs/dotnet/urho//mobile/ios
            cp "-r"  ${URHONET_HOME_ROOT}/template/libs/dotnet/urho//mobile/ios/*  libs/dotnet/urho//mobile/ios/
        fi

        if [ ! -d IOS ] ; then 
            verify_dir_exist_or_exit "${URHONET_HOME_ROOT}/template/IOS" 
            echo "copying IOS folder"

            cp -R ${URHONET_HOME_ROOT}/template/IOS .

            sed -i ""  "s*TEMPLATE_PROJECT_NAME*$PROJECT_NAME*g" "IOS/CMakeLists.txt"
            sed -i ""  "s*TEMPLATE_PROJECT_NAME*$PROJECT_NAME*g" "IOS/script/build_cli_ios.sh"
            sed -i ""  "s*TEMPLATE_UUID*$PROJECT_UUID*g" "IOS/script/build_cli_ios.sh"

            currPwd=`pwd`
            cd IOS
            mkdir bin
            cd bin
            ln -s  ../../Assets/* .
            cd $currPwd
        fi 

        if [ ! -d libs/ios ] ; then 
            cp -R ${URHONET_HOME_ROOT}/template/libs/ios libs
        fi 
    fi

    # copy relevant plugins
    mkdir -p IOS/Plugins
    for i in "${PLUGINS[@]}"
    do
        verify_dir_exist_or_exit "${URHONET_HOME_ROOT}/template/Plugins/${i}"
        if [ ! -d IOS/Plugins/${i} ] ; then 
            cp -R ${URHONET_HOME_ROOT}/template/Plugins/${i} IOS/Plugins/
        fi
    done

    # copy referenced DLLs to the IOS dotnet libs folder
    if [ -n "$DOTNET_REFERENCE_DLL" ] ; then
        for i in "${DOTNET_REFERENCE_DLL[@]}"
        do
            if [ -f ./References/${i} ]; then
                cp -f ./References/${i} ./libs/dotnet/urho/mobile/ios
            else 
                echo "${i} not found !!"
            fi
        done
    fi  

    cd IOS
    ./script/build_cli_ios.sh "$@"
    cd ..
else
	echo  "not an Apple platform , can't run"
	exit -1
fi