# Fetcher Tester

This document describes the configuration options for the CXML Fetcher Tester application.

## Table of Contents

- [Fetcher Tester](#fetcher-tester)
  - [Table of Contents](#table-of-contents)
  - [Configuration](#configuration)
  - [Individual Tests](#individual-tests)

## Configuration

### ENV: fetcher_tester_test_hostname

- Flag: test_hostname
- Required: Yes
- Description: The hostname to use for the local webhost/webhooks, for relevant tests.
- Default: N/A

### ENV: fetcher_tester_test_ip

- Flag: test_ip
- Required: Yes
- Description: The IP address to use for the local webhost/webhooks, for relevant tests.
- Default: N/A

### ENV: fetcher_tester_test_project_id

- Flag: test_project_id
- Required: Yes
- Description: The SignalWire project ID to use.
- Default: N/A

### ENV: fetcher_tester_test_space_id

- Flag: test_space_id
- Required: Yes
- Description: The SignalWire space ID to use.
- Default: N/A

### ENV: fetcher_tester_test_api_token

- Flag: test_api_token
- Required: Yes
- Description: The SignalWire security token to use.
- Default: N/A

### ENV: fetcher_tester_test_to_number

- Flag: test_to_number
- Required: Yes
- Description: The 'To Number' to use for dialed calls (required but not really used).
- Default: N/A

### ENV: fetcher_tester_test_from_number

- Flag: test_from_number
- Required: Yes
- Description: The 'From Number' to use for dialed calls (required but not really used).
- Default: N/A

### ENV: fetcher_tester_test_response

- Flag: test_response
- Required: No
- Description: The XML payload to return in OK test responses.
- Default: N/A

### ENV: fetcher_tester_test_delay_ms

- Flag: test_delay_ms
- Required: No
- Description: Delay in milliseconds for the "delay response" test.
- Default: 0
- Range: Greater than 0, Less than or Equal to 10000

### ENV: fetcher_tester_test_to_run

- Flag: test_to_run
- Required: No
- Description: Runs a specific test, if set- all tests will run otherwise.
- Default: N/A

## Individual Tests

A list of tests that can be set with `fetcher_tester_test_to_run`:

- fetch-hostname-test
- fetch-ip-test
- fetch-port-8080-test
- fetch-port-8080-ssl-test
- fetch-ssl-test
- fetch-ip-ssl-test
- fetch-delayed-test
