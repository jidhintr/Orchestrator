@UC-005
Feature: Client management

  Scenario: Create a client and read it back
    When I send a POST request to "/api/v1/clients" with body:
      """
      { "companyName": "Acme {unique}", "contactName": "Jane Doe", "email": "acme{unique}@example.com", "phone": "+15551234567", "shippingAddress": "1 Main St" }
      """
    Then the response status is 201
    And the response header "Location" is present
    And the response JSON "tier" is "Normal"
    And the response JSON "status" is "Active"
    And the response JSON "tierDiscountPercent" is "0"
    And the response JSON "isActive" is "true"
    And the response JSON "id" is at least 1000
    When I send a GET request to "/api/v1/clients/{id}"
    Then the response status is 200
    And the response JSON "companyName" is "Acme {unique}"
    When I send a GET request to "/api/v1/clients"
    Then the response list contains an item where "email" is "acme{unique}@example.com"

  Scenario: The tier sets the discount
    Given a client with email "gold{unique}@example.com" exists
    When I send a PUT request to "/api/v1/clients/{id}" with body:
      """
      { "companyName": "Gold {unique}", "contactName": "Jane Doe", "email": "gold{unique}@example.com", "phone": "+15551234567", "shippingAddress": "1 Main St", "tier": "Gold" }
      """
    Then the response status is 200
    And the response JSON "tier" is "Gold"
    And the response JSON "tierDiscountPercent" is "5"
    When I send a PUT request to "/api/v1/clients/{id}" with body:
      """
      { "companyName": "Gold {unique}", "contactName": "Jane Doe", "email": "gold{unique}@example.com", "phone": "+15551234567", "shippingAddress": "1 Main St", "tier": "Platinum" }
      """
    Then the response JSON "tierDiscountPercent" is "10"

  Scenario: Change the account status
    Given a client with email "pay{unique}@example.com" exists
    When I send a PUT request to "/api/v1/clients/{id}/status" with body:
      """
      { "status": "Suspended" }
      """
    Then the response status is 200
    And the response JSON "status" is "Suspended"
    And the response JSON "isActive" is "true"
    When I send a PUT request to "/api/v1/clients/{id}/status" with body:
      """
      { "status": "Inactive" }
      """
    Then the response JSON "status" is "Inactive"
    And the response JSON "isActive" is "false"

  Scenario: Duplicate email is rejected
    Given a client with email "dup{unique}@example.com" exists
    When I send a POST request to "/api/v1/clients" with body:
      """
      { "companyName": "Other {unique}", "contactName": "Jane Doe", "email": "DUP{unique}@example.com", "phone": "+15551234567", "shippingAddress": "1 Main St" }
      """
    Then the response is a problem with status 409

  Scenario: Invalid input is rejected
    When I send a POST request to "/api/v1/clients" with body:
      """
      { "companyName": "A", "contactName": "", "email": "not-an-email", "phone": "1", "shippingAddress": "", "tier": "Diamond" }
      """
    Then the response is a problem with status 400

  Scenario: Unknown status is rejected
    Given a client with email "bad{unique}@example.com" exists
    When I send a PUT request to "/api/v1/clients/{id}/status" with body:
      """
      { "status": "Gone" }
      """
    Then the response is a problem with status 400

  Scenario: Unknown client returns 404
    When I send a GET request to "/api/v1/clients/999999"
    Then the response is a problem with status 404
