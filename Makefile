PROJECT_NAME=my_fetcher_project
COMPOSE_FILE=docker-compose.yml

up:
	docker compose -p $(PROJECT_NAME) -f $(COMPOSE_FILE) up -d

build:
	docker compose -p $(PROJECT_NAME) -f $(COMPOSE_FILE) build

certs:
	./generate-certs.sh

test:
	@if [ -z "$(name)" ]; then \
		echo "Running all tests..."; \
		./run-tests.sh; \
	else \
		echo "Running test: $(name)"; \
		./run-tests.sh $(name); \
	fi
