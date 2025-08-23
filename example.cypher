CREATE (a:Person {name: $name})
CREATE (b:Person {name: $friendName})
CREATE (a)-[:KNOWS]->(b)