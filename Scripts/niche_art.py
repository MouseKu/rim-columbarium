"""Source geometry shared by eight-niche texture preparation and flower fitting."""

OPEN_SOURCE = 'open2.png'
CLOSED_SOURCE = 'close2.png'
# Union of the two alpha bounds: align visible outer frames, not empty margins.
FRAME_CROP = (36, 40, 1217, 1220)
FRAME_SIZE = (FRAME_CROP[2] - FRAME_CROP[0], FRAME_CROP[3] - FRAME_CROP[1])


def cropped_box(box):
    left, top, right, bottom = box
    return (left - FRAME_CROP[0], top - FRAME_CROP[1],
            right - FRAME_CROP[0], bottom - FRAME_CROP[1])


PLAQUE = cropped_box((404, 557, 850, 760))
INSCRIPTION = cropped_box((450, 603, 805, 713))

# Measured on the 118 x 118 compact closed-door artwork.
COMPACT_FRAME_SIZE = (118, 118)
COMPACT_PLAQUE = (25, 46, 94, 75)
COMPACT_INSCRIPTION = (39, 54, 79, 68)
