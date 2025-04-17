PROJECT_NAME=my_fetcher_project
COMPOSE_FILE=docker-compose.yml

up:
	docker compose -p $(PROJECT_NAME) -f $(COMPOSE_FILE) up -d

build:
	docker compose -p $(PROJECT_NAME) -f $(COMPOSE_FILE) build

test:
	./run-tests.sh
