# BerldBlackjack

Calculates the optimal play and expected value (EV) of one round of blackjack, dealt from a full shoe.
Every decision takes into account the exact cards seen (composition-dependent strategy).

## Rules

Set in `BerldBlackjack/Rules.cs`:

| Setting | Default |
|---|---|
| Decks | 6 |
| Double on any two cards | yes |
| Double after split | yes |
| Max hands from splitting | 4 (resplitting) |
| Resplit aces | no (split aces get one card) |

Fixed: dealer stands on soft 17, blackjack pays 3:2, no hole card (a dealer blackjack wins all bets,
including doubles and splits), no surrender.

## How it works

- `NodeBuilder` builds the game tree. Hands with the same cards share one node, whatever the order they
  were dealt in.
- `NodeEvaluator` takes the best option at player decisions and the probability-weighted average over
  cards drawn. The dealer's results are cached per set of cards.
- `SplitEvaluator` plays each split hand as its own tree and adds the hands up. Without resplitting this is
  exact. With resplitting, each hand is valued for the final number of hands (a small approximation).

## Results (6 decks, default rules)

| Moves allowed | EV |
|---|---|
| Hit and stand | -2.34% |
| Double any two + split with resplits | -0.51% |

## Verification

- Near-infinite deck (100,000 decks): matches an independent infinite-deck solver to within 1e-7, with and
  without doubling, splitting and resplitting.
- One deck: every split EV matches an independent composition-dependent evaluator to 1e-15.
- One deck, no resplitting: split EVs match an exact card-by-card enumeration of both hands and the dealer.
