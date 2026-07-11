# =============================================================================
# KnowledgeVault — Root Makefile
# =============================================================================

.PHONY: help setup up down status logs run-api run-ai run-web run-all smoke test clean nuke check-ports kill-ports

# Always use dev environment
ENV := dev

help:
	@$(MAKE) -f Makefile.dev help

setup:
	@$(MAKE) -f Makefile.dev setup

up:
	@$(MAKE) -f Makefile.dev up

down:
	@$(MAKE) -f Makefile.dev down

status:
	@$(MAKE) -f Makefile.dev status

logs:
	@$(MAKE) -f Makefile.dev logs

run-api:
	@$(MAKE) -f Makefile.dev run-api

run-ai:
	@$(MAKE) -f Makefile.dev run-ai

run-web:
	@$(MAKE) -f Makefile.dev run-web

run-all:
	@$(MAKE) -f Makefile.dev run-all

smoke:
	@$(MAKE) -f Makefile.dev smoke

test:
	@$(MAKE) -f Makefile.dev test

clean:
	@$(MAKE) -f Makefile.dev clean

nuke:
	@$(MAKE) -f Makefile.dev nuke

check-ports:
	@$(MAKE) -f Makefile.dev check-ports

kill-ports:
	@$(MAKE) -f Makefile.dev kill-ports