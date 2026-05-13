@startuml
    left to right direction
    :User: --> (Test Wi-Fi speed)
    :User: --> (See Wi-Fi channel being used by X APs)
    :User: --> (See BSSID)
    :User: --> (See SSIDs)
    :User: --> (See Wi-Fi mode)
        note right of (See Wi-Fi mode): Infrastructure or Ad-hoc.
    :User: --> (See signal strength)
        note right of (See signal strength): In dbm.
    :User: --> (See security protocol)
    :User: --> (See Wi-Fi spectrum)
    :User: --> (Compare with other tools)
    :User: --> (Change parameters being tested)
@enduml