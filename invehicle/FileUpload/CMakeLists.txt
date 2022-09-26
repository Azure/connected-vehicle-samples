# Steup the minimum version required of CMake to generate the Makefile.
cmake_minimum_required (VERSION 3.18)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Setup additional compiler related settings
set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")

# Setup the name of the project and the LANG of the code.
project(file-upload-module LANGUAGES CXX)

# Add a custom CMake Modules directory
set(CMAKE_MODULE_PATH ${CMAKE_CURRENT_LIST_DIR}/../../../shared-cpp/cmake ${CMAKE_MODULE_PATH})

find_package(AutomotiveEdge)

# Enable static code analysis for project
find_package(StaticCodeAnalysis)

set(Boost_USE_STATIC_LIBS ON)
find_package(Boost COMPONENTS REQUIRED system filesystem thread program_options regex)
add_definitions(-DBOOST_BEAST_USE_STD_STRING_VIEW)

find_package(nlohmann_json REQUIRED)

# Enable Curl for storage upload
set(CURL_LIBRARY "-lcurl") 
find_package(CURL REQUIRED) 
include_directories(${CURL_INCLUDE_DIR})

add_project(module_initialization ${CMAKE_CURRENT_LIST_DIR}/../common/module_initialization)
add_project(mcvp_data_contracts ${CMAKE_CURRENT_LIST_DIR}/../../../shared-cpp/data_contracts/mcvp)
add_project(module_data_contracts ${CMAKE_CURRENT_LIST_DIR}/../../../shared-cpp/data_contracts/modules)
add_project(mqtt_client ${CMAKE_CURRENT_LIST_DIR}/../../../shared-cpp/mqtt_client)
add_project(logging ${CMAKE_CURRENT_LIST_DIR}/../../../shared-cpp/logging)
add_project(data_contracts ${CMAKE_CURRENT_LIST_DIR}/../../../shared-cpp/data_contracts)
add_project(utils ${CMAKE_CURRENT_LIST_DIR}/../../../shared-cpp/utils)

# Setup the list of source files
set(PROJECT_HEADERS
  "${PROJECT_SOURCE_DIR}/processors/include/module_message_processor.h"
  "${PROJECT_SOURCE_DIR}/processors/include/upload_process_message.h"
  "${PROJECT_SOURCE_DIR}/processors/include/upload_processor.h"
  "${PROJECT_SOURCE_DIR}/processors/include/delete_processor.h"
  "${PROJECT_SOURCE_DIR}/handlers/include/blob_uri_handler.h"
  "${PROJECT_SOURCE_DIR}/handlers/include/blob_upload_handler.h"
)

set(PROJECT_SOURCES
  "${PROJECT_SOURCE_DIR}/main.cpp"
  "${PROJECT_SOURCE_DIR}/processors/module_message_processor.cpp"
  "${PROJECT_SOURCE_DIR}/processors/upload_processor.cpp"
  "${PROJECT_SOURCE_DIR}/processors/delete_processor.cpp"
  "${PROJECT_SOURCE_DIR}/handlers/blob_uri_handler.cpp"
  "${PROJECT_SOURCE_DIR}/handlers/blob_upload_handler.cpp"
)

# Calls the compiler
if(BUILD_FILE_UPLOAD_MODULE_AS_LIB)
  add_library(${PROJECT_NAME} main.cpp ${PROJECT_HEADERS} ${PROJECT_SOURCES})

  # Define headers for this library. PUBLIC headers are used for
  # compiling the library, and will be added to consumers' build
  # paths.
  target_include_directories(${PROJECT_NAME}
    PUBLIC
      $<BUILD_INTERFACE:${CMAKE_CURRENT_LIST_DIR}/include
    PRIVATE)
else() 
  add_executable(${PROJECT_NAME} ${PROJECT_HEADERS} ${PROJECT_SOURCES})
endif()

include_directories(${CMAKE_CURRENT_LIST_DIR}/../common/constants)

target_include_directories(${PROJECT_NAME}
    PUBLIC
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}>/processors/include
    $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}>/handlers/include
    $<INSTALL_INTERFACE:include>
)

# Link the libraries
target_link_libraries(${PROJECT_NAME}
  PUBLIC
  data_contracts
  mcvp_data_contracts
  module_data_contracts
  mqtt_client 
  PRIVATE
  ${Boost_LIBRARIES}
  module_initialization 
  logging
  utils
  nlohmann_json::nlohmann_json 
  ${CURL_LIBRARIES}
)

# Create a target specifically for cleanup.
add_custom_target(${PROJECT_NAME}_cleanup ALL)

# Additional files and directories to cleanup.
set_target_properties(${PROJECT_NAME}_cleanup PROPERTIES ADDITIONAL_CLEAN_FILES "CMakeCache.txt")