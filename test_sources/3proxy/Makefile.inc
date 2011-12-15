#$Id: Makefile.inc,v 1.2 2004/06/25 17:49:23 vlad Exp $
#
# 3 proxy common Makefile
#

all:
	$(TYPECOMMAND) $(MAKEFILE) > src/Makefile
	@cd src && $(MAKE)

clean:
	@$(REMOVECOMMAND) *$(OBJSUFFICS) $(COMPFILES)
	@cd src && $(MAKE) clean

