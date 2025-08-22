# Workshop Event-Sourcing

## Remerciements

Workshop adapté de celui de Jérémie Chassaing :  

- Blog: https://thinkbeforecoding.com
- Workshop d'origine : https://codeberg.org/thinkbeforecoding/es-workshop
- Blog post sur le pattern `Decider`: https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider

## Objectif

Migrer un système simple persisté sous la forme d'états vers une persistance event-sourcée.  

## Le système

Notre système va être une simple ampoule.  
Un utilisateur peut avoir 2 actions possibles sur celle-ci : l'allumer ou l'éteindre.  
Après un certain nombre de fois (par exemple 3) à avoir été allumée, l'ampoule casse.

## Étapes initiales

- Implémenter une ampoule 
- Implémenter une couche d'infrastructure pour charger et persister l'état de l'ampoule 
  - Stocker dans un fichier plat
  - On peut implémenter une CLI pour exécuter les commandes
- Si nécessaire : 
  - Définir un type `Commands`
  - Refactorer l'ampoule pour la transformer en une seule fonction `decide: State -> Commands -> State`
  - **Note:** Présenter *functional code/imperative shell*

## Étapes de migration

- Introduire les `Events` en boite noire
  - Définir des événements
  - Définir une nouvelle fonction `evolve: State -> Events -> State`
  - Modifier `decide` pour émettre des `Events` qu'il passe ensuite à la fonction `evolve`
  - **Note:** Permets de commencer à introduire les événements sans impacter la couche d'infrastructure
- On déplace l'appel de la fonction `evolve` dans la couche d'infrastructure
- On persiste les `Events` en plus de notre `State`
  - On utilise un fichier dédié pour les `Events`
  - On ajoute une fonction pour sérialiser nos `Events`
  - **Note:** Les `Events` sont exposés hors de la couche domaine -> on a une preuve que quelque chose c'est passé ou non
  - **Note:** Continuer à stocker l'état est très utile, car il permet de ne pas casser les lectures sur notre système, il peut également être apprécié par les analystes métiers qui sont toujours capables de voir l'état du système
- On charge les `Events`
  - On ajoute une fonction pour désérialiser nos `Events`
  - On modifie la couche infrastructure pour charger des `Events` au lieu d'un `State`
  - On doit définir un `initialState: State`
  - **Note:** On a un système event-sourcé
- On supprime la persistance du `State`

**Note:** On peut décider de stopper notre migration à n'importe laquelle de ces étapes. Celles-ci sont toutes des candidates valides pour être déployés.

## Questions pour Jérémie

- Une façon particulière d'introduire l'initial state ? Souvenir d'un event dédié `Built`.
