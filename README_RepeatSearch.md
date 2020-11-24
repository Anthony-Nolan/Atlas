# Summary

"Repeat Search" is a feature of Atlas that allows consumers to request differential results since the last time a request was run. 

To enable this, the Atlas system must store some state regarding which donors were previously returned, and when. 

In an effort to keep the algorithm logic stateless, this required state has been isolated to a standalone component responsible for managing repeat searches only.  