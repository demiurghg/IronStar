
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG;TRACE"
ASSEMBLY = ../tests/KopiLua.Tests.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Bin/Debug/net40/KopiLua.dll
BUILD_DIR = ../tests/

KOPILUA_TESTS_DLL_MDB_SOURCE=../tests/KopiLua.Tests.dll.mdb
KOPILUA_TESTS_DLL_MDB=$(BUILD_DIR)/KopiLua.Tests.dll.mdb
NUNIT_FRAMEWORK_DLL_SOURCE=../packages/NUnit.2.6.3/lib/nunit.framework.dll
KOPILUA_DLL_SOURCE=../Bin/Debug/net40/KopiLua.dll
KOPILUA_DLL_MDB_SOURCE=../Bin/Debug/net40/KopiLua.dll.mdb
KOPILUA_DLL_MDB=$(BUILD_DIR)/KopiLua.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ "-define:TRACE"
ASSEMBLY = ../tests/KopiLua.Tests.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Bin/Release/net40/KopiLua.dll
BUILD_DIR = ../tests/

KOPILUA_TESTS_DLL_MDB=
NUNIT_FRAMEWORK_DLL_SOURCE=../packages/NUnit.2.6.3/lib/nunit.framework.dll
KOPILUA_DLL_SOURCE=../Bin/Release/net40/KopiLua.dll
KOPILUA_DLL_MDB=

endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(KOPILUA_TESTS_DLL_MDB) \
	$(NUNIT_FRAMEWORK_DLL) \
	$(KOPILUA_DLL) \
	$(KOPILUA_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(KOPILUA_TESTS_40_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	../tests/core.cs \
	Properties/AssemblyInfo.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	Properties \
	packages.config \
	kopilua.tests.40.pc.in 

REFERENCES =  \
	System \
	System.Core

DLL_REFERENCES =  \
	../lib/nunit/nunit.framework.dll

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

NUNIT_FRAMEWORK_DLL = $(BUILD_DIR)/nunit.framework.dll
KOPILUA_DLL = $(BUILD_DIR)/KopiLua.dll
KOPILUA_TESTS_40_PC = $(BUILD_DIR)/kopilua.tests.40.pc

$(eval $(call emit-deploy-target,NUNIT_FRAMEWORK_DLL))
$(eval $(call emit-deploy-target,KOPILUA_DLL))
$(eval $(call emit-deploy-target,KOPILUA_DLL_MDB))
$(eval $(call emit-deploy-wrapper,KOPILUA_TESTS_40_PC,kopilua.tests.40.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
