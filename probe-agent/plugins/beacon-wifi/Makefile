PLUGIN=beacon-wifi
SRC=$(PLUGIN).c

ifeq ($(OS),Windows_NT)
EXE=$(PLUGIN).exe
else
EXE=$(PLUGIN)
endif

.PHONY: all windows-gcc windows-cl linux clean zip

all: $(EXE)

$(EXE): $(SRC)
ifeq ($(OS),Windows_NT)
	gcc -std=c99 -O2 -Wall -Wextra -o $(EXE) $(SRC) -lwlanapi -lole32 -liphlpapi
else
	gcc -std=c99 -O2 -Wall -Wextra -o $(EXE) $(SRC)
endif

windows-gcc:
	gcc -std=c99 -O2 -Wall -Wextra -o $(PLUGIN).exe $(SRC) -lwlanapi -lole32 -liphlpapi

windows-cl:
	cl /nologo /W4 /O2 /Fe:$(PLUGIN).exe $(SRC) wlanapi.lib ole32.lib iphlpapi.lib

linux:
	gcc -std=c99 -O2 -Wall -Wextra -o $(PLUGIN) $(SRC)

zip:
	powershell -NoProfile -Command "Compress-Archive -Path manifest.json,plugin.json,$(SRC),Makefile,$(PLUGIN).exe -DestinationPath ..\\..\\..\\code\\central-server\\plugin-bundles\\$(PLUGIN)-1.0.0.zip -Force"

clean:
	-del /Q $(PLUGIN).exe 2>NUL
	rm -f $(PLUGIN)
