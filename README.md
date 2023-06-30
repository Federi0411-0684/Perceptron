# Perceptron
Basic image classifier (W.I.P.)

Il Perceptron fu la prima macchina in grado di riconoscere le immagini, ideata nel 1943 e costruita da Frank Rosenblatt nel 1958. Oggi è considerato uno dei precursori dei moderni modelli di classificazione.

Questa applicazione Windows, realizzata in *C#* con *.NET* e *windows forms*, utilizza un modello di intelligenza artificiale sviluppato dalle fondamenta in modo intuitivo, basandosi sul funzionamento del perceptron, per riconoscere, una volta effettuato un allenamento supervisionato, il soggetto di una data immagine.

Il principio di funzionamento di base è il seguente:
Partendo da una matrice di zeri e dei dataset di foto etichettati, il modello viene allenato scorrendo tutte le immagini e modificando la matrice (maschera) in base alla valutazione della singola foto presa in considerazione.

---
La valutazione è molto semplice; consiste nell'eseguire il prodotto scalare tra la foto corrente e la maschera: il risultato viene passato alla funzione di attivazione, che stabilisce il livello di somiglianza tra la maschera e l'immagine data. Il riconoscimento di più classi di soggetti è effettuato utilizzando una maschera per ogni classe, in modo da valutare l'immagine da classificare rispetto a ognuna di esse, quindi considerando le corrispondenze migliori.
