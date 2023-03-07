
#!/usr/bin/env bash

# Copyright (c) 2021-2022 Eli Aloni a.k.a elix22.
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

CWD=$(pwd)
unamestr=$(uname)
# Switch-on alias expansion within the script 
shopt -s expand_aliases

#Alias the sed in-place command for OSX and Linux - incompatibilities between BSD and Linux sed args
if [[ "$unamestr" == "Darwin" ]]; then
	alias aliassedinplace='sed -i ""'
else
	#For Linux, notice no space after the '-i' 
	alias aliassedinplace='sed -i""'
fi


verify_dir_exist_or_exit()
{
    if [ ! -d $1 ] ; then
        echo "$1 not found , are you sure that URHONET_HOME_ROOT=${URHONET_HOME_ROOT} is pointing to the right place ? "
        exit 1
    fi
}

if [ ! -f ~/.urhonet_config/urhonethome ]; then
	echo  "1 Urho.Net is not configured , please  run configure.sh (configure.bat on Windows) from  the Urho.Net installation folder  "
	exit -1
fi

URHONET_HOME_ROOT=$(cat ~/.urhonet_config/urhonethome)

if [ ! -d "$URHONET_HOME_ROOT" ]; then
	echo  "Urho.Net is not configured , please  run configure.sh (configure.bat on Windows) from the Urho.Net installation folder  "
	exit -1
else
    echo  "URHONET_HOME_ROOT=$URHONET_HOME_ROOT"

if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
    export PYTHON=${URHONET_HOME_ROOT}/tools/python/windows/python.exe
elif [[ "$OSTYPE" == "darwin"* ]]; then
    export PYTHON=${URHONET_HOME_ROOT}/tools/python/macos/bin/python3
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    export PYTHON=${URHONET_HOME_ROOT}/tools/python/linux/bin/python3
elif [[ "$OSTYPE" == "freebsd"* ]]; then
    export PYTHON=${URHONET_HOME_ROOT}/tools/python/linux/bin/python3
fi 

    if [ ! -d Web ] ; then 
            mkdir -p Web
            cp -R ${URHONET_HOME_ROOT}/template/libs/web/* Web/
    fi 

    dotnet build --configuration Release -p:DefineConstants=_WEB_

    rm -rf Assets/Data/DotNet/ios

    dotnet tools/ReferenceAssemblyResolver/ReferenceAssemblyResolver.dll --assembly Assets/Data/DotNet/Game.dll   --output Web/DotNet  --search ${URHONET_HOME_ROOT}/template/libs/dotnet/urho/web,${URHONET_HOME_ROOT}/template/libs/dotnet/bcl/wasm,${URHONET_HOME_ROOT}/template/libs/dotnet/bcl/wasm/Facades

    if [ -n "$DOTNET_REFERENCE_DLL" ] ; then
            for i in "${DOTNET_REFERENCE_DLL[@]}"
            do
                if [ -f ./References/${i} ]; then
                    cp -f ./References/${i} ./Web/DotNet
                else 
                    echo "${i} not found !!"
                fi
            done
    fi 

    cp -f ${URHONET_HOME_ROOT}/template/libs/dotnet/bcl/wasm/netstandard.dll ./Web/DotNet
    
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    # TBD ELI Compression fails on linux 
    ${PYTHON} ${URHONET_HOME_ROOT}/tools/ems_tools/tools/file_packager.py  Web/UrhoNetFileSystem.data  --preload "Assets@/"  --preload "Web/DotNet@/Data/DotNet/web"    --js-output=Web/UrhoNetFileSystemPreloader.js --use-preload-cache
else
    ${PYTHON} ${URHONET_HOME_ROOT}/tools/ems_tools/tools/file_packager.py  Web/UrhoNetFileSystem.data  --preload "Assets@/"  --preload "Web/DotNet@/Data/DotNet/web"    --js-output=Web/UrhoNetFileSystemPreloader.js --use-preload-cache --lz4
fi
    rm -rf Web/DotNet
fi