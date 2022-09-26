ARG container_registry=mcr.microsoft.com/mirror/docker/library
ARG base_image=iotccplazeacrtest.azurecr.io/autoedge-base
ARG base_tag=2.12.11200.607-linux-amd64-ubuntu2004
FROM ${base_image}:${base_tag} AS build

# By default the code is automatically formatted, setting this to ON will have "make" to validate the code
# and throw an error if the code is not compliant
# Used in the CI/CD to ensure code compliance
ARG code_analysis_validate_code_only="OFF"
ARG ignore_code_analysis="OFF"
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
RUN rm -rf ./* \
    && cmake .. ${cmake_args} -DCMAKE_BUILD_TYPE="${cmake_build_type}" \
    && make -j$(nproc)

# Copy binaries from one image to another.
FROM ${container_registry}/ubuntu:20.04
WORKDIR /bin

RUN groupadd -r autoedge -g 2000 \
    && useradd -ms /bin/bash moduleuser -u 2001 -G autoedge \
    && apt-get -y update --fix-missing && apt-get install -y --no-install-recommends \
    libcurl3-gnutls ca-certificates \
    && rm -rf /var/lib/apt/lists/*

USER moduleuser

COPY --from=build /usr/local/lib/libwolfssl.so /usr/local/lib/libwolfssl.so.32 /usr/local/lib/libwolfssl.so.32.0.0 /usr/local/lib/
COPY --from=build /temp/workloads/modules/file_upload_module/build/file-upload-module /bin/

# Add ENTRYPOINT to run image as executable.
ENTRYPOINT ["/bin/file-upload-module"]
