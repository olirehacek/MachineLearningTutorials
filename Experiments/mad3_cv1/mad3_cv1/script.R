## MAD 3 - Cvi�en� 1 (25/9/17)
## Ond�ej �eh��ek - REH0063

install.packages("igraph")
library(igraph)

## --------------------1--------------------

## 1a) Random graph + plot
rand <- sample_gnm(n = 800, m = 600)
plot(rand, vertex.size = 2, vertex.label = NA)

## 1b) Barab�si�Albert model + plot
bara <- barabasi.game(200, power = 1.0, m = 600)
plot(g, vertex.label = NA, vertex.size = 2)

## --------------------2--------------------

## 2) Matice sousednosti
as_adjacency_matrix(rand)
as_adjacency_matrix(bara)

## 2c) Pr�m�rn� st�edn� hodnota nejkrat�� cesty
mean_distance(rand)
mean_distance(bara)

## 2d) Stupn� jednotliv�ch vrchol� + max stupe�
degree(rand)
max(degree(rand))

degree(bara)
max(degree(bara))

## --------------------3--------------------

## 3a) Cilen� odeb�rn� maxim�ln�ho stupn�
subgraph_maxdegree_rand_1 = delete_vertices(rand, max(degree(rand)))
subgraph_maxdegree_bara_1 = delete_vertices(bara, max(degree(bara)))

## 3a) Cilen� odeb�rn� nahodn�ho stupn�
subgraph_random_rand_1 = delete_vertices(rand, 3)
subgraph_random_bara_1 = delete_vertices(bara, 3)

