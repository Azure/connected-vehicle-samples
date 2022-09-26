ARG base_image=iotccplazeacrtest.azurecr.io/autoedge-base
ARG base_tag=2.12.11200.607-linux-aarch64-cc-ubuntu2004
FROM ${base_image}:${base_tag} AS build

# By default the code is automatically formatted, setting this to ON will have "make" to validate the code
# and throw an error if the code is not compliant
# Used in the CI/CD to ensure code compliance
ARG code_analysis_validate_code_only="OFF"
ARG ignore_code_analysis="ON"
ARG cmake_build_type="Release"
ARG cmake_args="-DRUN_ONLY_CODE_ANALYSIS=${code_analysis_validate_code_only} -DIGNORE_CODE_ANALYSIS=${ignore_code_analysis}"
ARG cache_dir="~"
ENV CCACHE_DIR="${cache_dir}/ccache"
ENV STATIC_CHECK_CACHE_DIRECTORY="${cache_dir}/static_checks_cache"

# Build the file-upload-module.
COPY src/ /temp/
COPY .clang-format /temp/
COPY .clang-tidy /temp/
WORKDIR /temp/workloads/modules/file_upload_module/build

######################################################################################################################
# The following code is commented on purpose and is a placeholder for the aarch64 file upload module to be compiled
# This image if build won't work as is
######################################################################################################################
# RUN rm -rf ./* \
#     && cmake .. ${cmake_args} -DCMAKE_BUILD_TYPE="${cmake_build_type}"  -DCMAKE_SYSROOT="${SYSROOT}" -DCMAKE_C_COMPILER="/${TOOLCHAIN}/bin/aarch64-linux-gnu-gcc" -DCMAKE_CXX_COMPILER="/${TOOLCHAIN}/bin/aarch64-linux-gnu-g++" -DCMAKE_SYSTEM_NAME=Linux -DCMAKE_SYSTEM_VERSION=1 -DARCH="aarch64" \
#     && make -j$(nproc)

# This Dockerfile exists as running arm64 architecture based docker images don't run on amd64 build agents natively.
# This Dockerfile should be built on an arm64 device directly and can later be consumed by the modules container's Dockerfile as a base image
# TODO: Move to Alpine later
FROM iotccplazeacrtest.azurecr.io/workloads-runtime-base:1.0-aarch64 AS runtime
WORKDIR /bin

# COPY --from=build /temp/workloads/modules/file_upload_module/build/file-upload-module /bin/

# Add ENTRYPOINT to run image as executable.
# ENTRYPOINT ["/bin/file-upload-module"]
