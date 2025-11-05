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

## Déroulé

### Étapes initiales

- Implémenter une ampoule avec pour contraintes (**15min**):  
  - Définir un type `Commands`
  - Une seule fonction `decide: State -> Commands -> State`
- Implémenter une couche d'infrastructure pour charger et persister l'état de l'ampoule (**25min**):  
  - Stocker dans un fichier plat
  - On peut implémenter une CLI pour exécuter les commandes
  - **Note:** Présenter *functional code/imperative shell*

### Étapes de migration vers *Event-Sourcing*

- Introduire les `Events` en boite noire (**25min**):
  - Définir des événements
  - Définir une nouvelle fonction `evolve: State -> Events -> State`
  - Modifier `decide` pour émettre des `Events` qu'il passe ensuite à la fonction `evolve`
  - **Note:** Permets de commencer à introduire les événements sans impacter la couche d'*imperative shell*
- On déplace l'appel de la fonction `evolve` dans la couche d'infrastructure (**10min**):
  - **Note:** Change le contrat de l'agrégat, simple dans le workshop mais nécessite de modifier les tests  
- On persiste les `Events` en plus de notre `State` (**20min**):
  - On utilise un fichier dédié pour les `Events`
  - On ajoute une fonction pour sérialiser nos `Events`
  - **Note:** Les `Events` sont exposés hors de la couche domaine -> on a une preuve que quelque chose c'est passé ou non
- On charge les `Events` (**15min**):
  - On ajoute une fonction pour désérialiser nos `Events`
  - On modifie la couche infrastructure pour charger des `Events` au lieu d'un `State`
  - On doit définir un `initialState: State`
  - **Note:** Pour l'initial state on peut choisir une valeur par défault, ou alors il faut un état spécial (par exemple `NotBuild` associé à une première commande et event pour initialiser les valeurs: `Build` & `Built`)
  - **Note:** On a un système event-sourcé
  - **Note:** Dans un contexte de production, penser à une vrai stratégie de sérialization et faire attention au design de ses `Events` car les payloads doivent pouvoir être utilisé dans le temps
  - **Note:** Continuer à stocker l'état est très utile, car il permet de ne pas casser les lectures sur notre système, il peut également être apprécié par les analystes métiers qui sont toujours capables de voir l'état du système
- On supprime la persistance du `State` (**5min**):

**Note:** On peut décider de stopper notre migration à n'importe laquelle de ces étapes. Celles-ci sont toutes des candidates valides pour être déployés.  
**Note:** Du moment que l'on a `Broke` comme event, on peut avoir notre ampoule en `isTerminal = true`.

### Étapes de généralisation du pattern

- Rendre `EventStore` générique (**5min**)
- Introduire le `Decider<'Commands, 'Events, 'State>` et créer un `bulbDecider: Decider<'Commands, 'Events, 'State>` (**15min**).
- Gestion de la concurrence (**15min**) :  
  - Ajout d'une version pour le chargement et sauvegarde des événements.
  - Contrôle de la version à la sauvegarde : le nombre d'événements dans l'event store doit correspondre à la version fournie.
  - **Note:** Avec le fichier plat, on ne peut pas ajouter les `Events` et vérifier la version de façon atomique (sans utiliser un lock), on émet l'hypothèse que c'est le cas pour ce workshop

### CQRS/ES

- *CQRS*: Command and Query Responsability Segregation, usage de modèles différents pour l'écriture et les lectures
- Jusqu'ici, le workshop c'est concentré sur la partie *Command*
- Nécessité de maintenir des modèles dédiés à la lecture: les `Events` ne sont pas vraiment adaptés
- Si le save est réussi, alors on projète les événements
