@UC-008
Feature: Orders

  An order is placed for a client against an active catalog, starts PendingApproval and keeps an append-only event history.
  Managers cancel it (it returns to the Estimation department) or request a priority change. Client users only see their own orders.

  Background:
    Given a client and a catalog exist
    And a user "Ann" exists in the departments "UNASSIGNED" with the roles "Manager"
    And a user "Cal" exists in the departments "UNASSIGNED" with the roles ""

  Scenario: Place an order and read it back with its history
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 5 }], "priorityId": 2, "reference": "PO-{unique}" }
      """
    Then the response status is 201
    And the response header "Location" is present
    And the response JSON "status" is "PendingApproval"
    And the response JSON "priorityName" is "High"
    And the response JSON "subTotal" is present
    And the response JSON "reference" is "PO-{unique}"
    And the response JSON "id" is at least 1000
    And the response JSON "orderNo" is present
    When I send a GET request to "/api/v1/orders/{id}"
    Then the response status is 200
    And the response JSON "clientCompanyName" is present
    When I send a GET request to "/api/v1/orders/{id}/events"
    Then the response list contains an item where "type" is "Placed"
    When I send a GET request to "/api/v1/orders"
    Then the response list contains an item where "status" is "PendingApproval"

  Scenario: Staff on behalf of a client may place an order
    Given I act as "Cal"
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 5 }] }
      """
    Then the response status is 201
    And the response JSON "priorityName" is "Normal"

  Scenario: Invalid orders are rejected
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "items": [{ "catalogId": {catalog:current}, "quantity": 5 }] }
      """
    Then the response is a problem with status 400
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": 999999, "quantity": 5 }] }
      """
    Then the response is a problem with status 400
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 5 }], "priorityId": 999999 }
      """
    Then the response is a problem with status 400

  Scenario: A suspended client cannot place an order
    When I send a PUT request to "/api/v1/clients/{client:current}/status" with body:
      """
      { "status": "Suspended" }
      """
    And I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 5 }] }
      """
    Then the response is a problem with status 409

  Scenario: A manager cancels an order and it returns to the Estimation department
    Given an order is placed for that client and catalog
    And I act as "Ann"
    When I send a POST request to "/api/v1/orders/{order:current}/cancel" with body:
      """
      { "reason": "Stock deficit" }
      """
    Then the response status is 200
    And the response JSON "status" is "Cancelled"
    And the response JSON "returnedToDepartmentId" is "8"
    When I send a GET request to "/api/v1/orders/{order:current}/events"
    Then the response list contains an item where "type" is "Cancelled"
    When I send a POST request to "/api/v1/orders/{order:current}/cancel" with body:
      """
      { "reason": "Again" }
      """
    Then the response is a problem with status 409

  Scenario: An employee cannot cancel an order
    Given an order is placed for that client and catalog
    And I act as "Cal"
    When I send a POST request to "/api/v1/orders/{order:current}/cancel" with body:
      """
      { "reason": "Quality failure" }
      """
    Then the response is a problem with status 403
    When I send a GET request to "/api/v1/orders/{order:current}"
    Then the response JSON "status" is "PendingApproval"

  Scenario: A priority change request is logged and the priority stays until approved
    Given an order is placed for that client and catalog
    And I act as "Ann"
    When I send a POST request to "/api/v1/orders/{order:current}/priority-requests" with body:
      """
      { "priorityId": 4, "reason": "Rush job" }
      """
    Then the response status is 201
    And the response JSON "type" is "PriorityChangeRequested"
    And the response JSON "toValue" is "Ultra High"
    When I send a GET request to "/api/v1/orders/{order:current}"
    Then the response JSON "priorityName" is "Normal"

  Scenario: A priority change to the current priority is rejected
    Given an order is placed for that client and catalog
    And I act as "Ann"
    When I send a POST request to "/api/v1/orders/{order:current}/priority-requests" with body:
      """
      { "priorityId": 1 }
      """
    Then the response is a problem with status 400

  Scenario: A client user places and sees only their own orders
    Given an order is placed for that client and catalog
    And a client user "Cli" exists
    And I act as "Cli"
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "items": [{ "catalogId": {catalog:current}, "quantity": 5 }] }
      """
    Then the response status is 201
    And the response JSON "clientId" is "{client:Cli}"
    When I send a GET request to "/api/v1/orders"
    Then the response list does not contain an item where "clientId" is "{client:current}"
    When I send a GET request to "/api/v1/orders/{order:current}"
    Then the response is a problem with status 404

  Scenario: A client user cannot cancel orders
    Given a client user "Cli" exists
    And I act as "Cli"
    And an order is placed for that client and catalog
    When I send a POST request to "/api/v1/orders/{order:current}/cancel" with body:
      """
      { "reason": "Changed my mind" }
      """
    Then the response is a problem with status 403

  Scenario: Anonymous callers are rejected
    Given I act as nobody
    When I send a GET request to "/api/v1/orders"
    Then the response is a problem with status 401

  Scenario: Unknown order returns 404
    When I send a GET request to "/api/v1/orders/999999"
    Then the response is a problem with status 404

  Scenario: The quote is computed at placement from the catalog base price and discounts
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 10 }] }
      """
    Then the response status is 201
    And the response JSON "subTotal" is present
    And the response JSON "netAmount" is present
    And the response body does not contain "laborCost"
    And the response body does not contain "commission"

  Scenario: An item carries a description and specs
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 5, "description": "Gift box", "specs": [{ "name": "Size", "value": "A4" }] }] }
      """
    Then the response status is 201
    And the response JSON "items.0.description" is "Gift box"
    And the response JSON "items.0.specs.0.name" is "Size"
    And the response JSON "items.0.specs.0.value" is "A4"

  Scenario: A spec without a value is rejected
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 5, "specs": [{ "name": "Size", "value": "" }] }] }
      """
    Then the response is a problem with status 400

  Scenario: Invalid quantity or past due date is rejected
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 0 }] }
      """
    Then the response is a problem with status 400
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 1 }], "commitmentDeadline": "2020-01-01" }
      """
    Then the response is a problem with status 400

  Scenario: Estimation adjusts the quote and it is logged
    Given an order is placed for that client and catalog
    When I send a POST request to "/api/v1/orders/{order:current}/quote" with body:
      """
      { "total": 123.45, "reason": "Bulk rate" }
      """
    Then the response status is 200
    And the response JSON "netAmount" is "123.45"
    When I send a GET request to "/api/v1/orders/{order:current}/events"
    Then the response list contains an item where "type" is "QuoteAdjusted"

  Scenario: A manager outside Estimation cannot adjust the quote
    Given an order is placed for that client and catalog
    And I act as "Ann"
    When I send a POST request to "/api/v1/orders/{order:current}/quote" with body:
      """
      { "total": 1, "reason": "Goodwill" }
      """
    Then the response is a problem with status 403

  Scenario: Priorities are listed by rank
    When I send a GET request to "/api/v1/priorities"
    Then the response status is 200
    And the response list contains an item where "name" is "Ultra High"

  Scenario: The same catalog twice in one order is rejected
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {catalog:current}, "quantity": 1 }, { "catalogId": {catalog:current}, "quantity": 2 }] }
      """
    Then the response is a problem with status 400

  Scenario: An order without items is rejected
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [] }
      """
    Then the response is a problem with status 400

  Scenario: An unapproved catalog cannot be ordered
    When I send a POST request to "/api/v1/catalogs" with body:
      """
      { "sku": "UNA{unique}", "name": "Unapproved {unique}", "jobTypeIds": [1] }
      """
    Then the response status is 201
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {id}, "quantity": 1 }] }
      """
    Then the response is a problem with status 409

  Scenario: A catalog cannot be ordered outside its availability window
    When I send a POST request to "/api/v1/catalogs" with body:
      """
      { "sku": "OUT{unique}", "name": "Future {unique}", "availableFrom": "2099-01-01", "jobTypeIds": [1] }
      """
    Then the response status is 201
    When I send a POST request to "/api/v1/catalogs/{id}/approve"
    Then the response status is 200
    When I send a POST request to "/api/v1/orders" with body:
      """
      { "clientId": {client:current}, "items": [{ "catalogId": {id}, "quantity": 1 }] }
      """
    Then the response is a problem with status 409
