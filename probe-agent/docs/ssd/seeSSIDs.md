@startuml
    autonumber
    autoactivate on
    actor user as "User"
    box "Raspberry Pi 5"
        participant rasp as "Raspberry Pi 5"
        participant script as "Start Up Script"
        participant c as "C Program"
    end box
    box "Server"
        participant prom as "Prometheus"
    end box
    user -> rasp : boots up device
    rasp -> script : runs script on boot up
    script -> c : script runs the c program
    script --> rasp : returns success/failure running the c program
    c -> c : starts wifi scan
    c -> rasp : opens port so that prometheus can consume the SSIDs data
    prom -> rasp : consumes data in port
    user -> prom : checks data
@enduml