
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:TRACE;DEBUG;LUA_CORE;CATCH_EXCEPTIONS"
ASSEMBLY = ../Bin/Debug/net40/Lua.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../Bin/Debug/net40/KopiLua.dll
BUILD_DIR = ../Bin/Debug/net40/

LUA_EXE_MDB_SOURCE=../Bin/Debug/net40/Lua.exe.mdb
LUA_EXE_MDB=$(BUILD_DIR)/Lua.exe.mdb
LUA_EXE_CONFIG_SOURCE=app.config
KOPILUA_DLL_SOURCE=../Bin/Debug/net40/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../Bin/Debug/net40/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE;LUA_CORE;CATCH_EXCEPTIONS"
ASSEMBLY = ../Bin/Release/net40/Lua.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../Bin/Release/net40/KopiLua.dll
BUILD_DIR = ../Bin/Release/net40/

LUA_EXE_MDB=
LUA_EXE_CONFIG_SOURCE=app.config
KOPILUA_DLL_SOURCE=../Bin/Release/net40/KopiLua.dll
KOPILUA_DLL_MDB=

endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(LUA_EXE_MDB) \
	$(LUA_EXE_CONFIG) \
	$(KOPILUA_DLL) \
	$(KOPILUA_DLL_MDB)  

BINARIES = \
	$(LUA_40)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	lua.cs \
	Properties/AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	app.config \
	lua.40.in 

REFERENCES =  \
	Accessibility \
	System \
	System.Core \
	System.Data

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

LUA_EXE_CONFIG = $(BUILD_DIR)/Lua.exe.config
KOPILUA_DLL = $(BUILD_DIR)/KopiLua.dll
LUA_40 = $(BUILD_DIR)/lua.40

$(eval $(call emit-deploy-target,LUA_EXE_CONFIG))
$(eval $(call emit-deploy-wrapper,LUA_40,lua.40,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
