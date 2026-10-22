@UC-004
Feature: Catalog management

  Scenario: Create a catalog with an ordered job type sequence and read it back
    When I send a POST request to "/api/v1/catalogs" with body:
      """
      { "sku": "BAG{unique}", "name": "Printed Bags {unique}", "basePrice": 10.5, "laborCost": 2, "commissionPercent": 5, "profitPercent": 20, "jobTypeIds": [1, 2, 4] }
      """
    Then the response status is 201
    And the response header "Location" is present
    And the response JSON "sku" is "BAG{unique}"
    And the response JSON "isActive" is "true"
    And the response JSON "id" is at least 1000
    And the catalog job types are "Cutting,Laminating,Delivery"
    When I send a GET request to "/api/v1/catalogs/{id}"
    Then the response status is 200
    And the catalog job types are "Cutting,Laminating,Delivery"
    When I send a GET request to "/api/v1/catalogs"
    Then the response list contains an item where "sku" is "BAG{unique}"

  Scenario: Clone a catalog and reorder the clone without touching the original
    Given a catalog with SKU "BAG{unique}" exists
    When I send a POST request to "/api/v1/catalogs/{id}/clone" with body:
      """
      { "sku": "PRM{unique}", "name": "Premium Bags {unique}" }
      """
    Then the response status is 201
    And the response JSON "name" is "Premium Bags {unique}"
    And the catalog job types are "Cutting,Laminating,Quality Assurance"
    When I send a PUT request to "/api/v1/catalogs/{id}" with body:
      """
      { "sku": "PRM{unique}", "name": "Premium Bags {unique}", "jobTypeIds": [3, 1] }
      """
    Then the response status is 200
    And the catalog job types are "Quality Assurance,Cutting"
    When I send a GET request to "/api/v1/catalogs"
    Then the response list contains an item where "sku" is "BAG{unique}"

  Scenario: A catalog with orders cannot be edited
    Given a catalog with SKU "BAG{unique}" exists
    And an order exists for the catalog
    When I send a PUT request to "/api/v1/catalogs/{id}" with body:
      """
      { "sku": "BAG{unique}", "name": "Renamed {unique}", "jobTypeIds": [1] }
      """
    Then the response is a problem with status 409
    When I send a GET request to "/api/v1/catalogs/{id}"
    Then the catalog job types are "Cutting,Laminating,Quality Assurance"

  Scenario: A catalog with orders cannot be deleted
    Given a catalog with SKU "BAG{unique}" exists
    And an order exists for the catalog
    When I send a POST request to "/api/v1/catalogs/{id}/deactivate"
    Then the response is a problem with status 409
    When I send a GET request to "/api/v1/catalogs/{id}"
    Then the response JSON "isActive" is "true"

  Scenario: A catalog with orders can still be cloned
    Given a catalog with SKU "BAG{unique}" exists
    And an order exists for the catalog
    When I send a POST request to "/api/v1/catalogs/{id}/clone" with body:
      """
      { "sku": "NEW{unique}", "name": "New Version {unique}" }
      """
    Then the response status is 201

  Scenario: Deactivate a catalog without orders
    Given a catalog with SKU "BAG{unique}" exists
    When I send a POST request to "/api/v1/catalogs/{id}/deactivate"
    Then the response status is 204
    When I send a GET request to "/api/v1/catalogs/{id}"
    Then the response JSON "isActive" is "false"

  Scenario: Duplicate SKU is rejected
    Given a catalog with SKU "BAG{unique}" exists
    When I send a POST request to "/api/v1/catalogs" with body:
      """
      { "sku": "bag{unique}", "name": "Other {unique}", "jobTypeIds": [1] }
      """
    Then the response is a problem with status 409

  Scenario: Invalid input is rejected
    When I send a POST request to "/api/v1/catalogs" with body:
      """
      { "sku": "X", "name": "", "profitPercent": 150, "jobTypeIds": [] }
      """
    Then the response is a problem with status 400

  Scenario: Unknown job type is rejected
    When I send a POST request to "/api/v1/catalogs" with body:
      """
      { "sku": "BAD{unique}", "name": "Bad {unique}", "jobTypeIds": [1, 999999] }
      """
    Then the response is a problem with status 400

  Scenario: Unknown catalog returns 404
    When I send a GET request to "/api/v1/catalogs/999999"
    Then the response is a problem with status 404
