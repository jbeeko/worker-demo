module Contacts
open Thoth.Json

open WorkersInterop

type Contact = {
    id: string
    FirstName: string
    FamillyName: string
    DOB: string
    EMail: string
} with
    member x.Name = System.String.Join(" ", [|x.FirstName.Trim(); x.FamillyName.Trim()|])
let  contactDecoder = Decode.Auto.generateDecoder<Contact>()
let nsGet = KVStore.get "eac7f3cf92a24ebab3b900a86df7d787"


// Handle the request returning a ServiceWorker Response promise
let rec routeRequest (verb: Verb) (path: string list) (req: CFWRequest) =
    match (verb, path) with
    | GET, [i] ->   getContact req i
    | GET, [] ->    getContacts req
    | POST, [] ->   postContact req
    | PUT, [i] ->   putContact req i
    | DELETE, [i] ->   deleteContact req i
    | _, _ ->       noHandler req

and private getContact req i =
    promise {
        match! KVStore.get i with 
        | None -> return newResponse (sprintf "No contact with id %s" i) "404"
        | Some json -> return newResponse json "200"
    }
and private getContacts req  =
    promise {
        // let! keys = kvKeyListing()
        // let len = keys.success.ToString()
        return newResponse "len" "200"
    }

and private postContact req  =
    promise {
         let! body = (req.text())
         let contact = Decode.fromString contactDecoder body
         match contact with
         | Ok c -> 
            do! KVStore.put c.id body
            return newResponse body "200"
         | Error e -> return newResponse (sprintf "Unable to process: %s because: %O" body e) "200"
    }

and private putContact req i =
    promise {
        let! body = (req.text())
        let updatedContact = Decode.fromString contactDecoder body
        match! KVStore.get i with
        | Some d -> 
            let existingContact = Decode.fromString contactDecoder d
            match existingContact, updatedContact with
            | Ok existing, Ok updated -> 
               if existing.id = i 
               then
                   do! KVStore.put i body
                   return newResponse body "200"
               else return newResponse (sprintf "Unable to process put.") "400"             
            | _,_ -> return newResponse (sprintf "Unable to process put.") "400"
        | None -> return newResponse (sprintf "Contact not found %s" i) "404"
    }

and private deleteContact req i  =
    promise {
        match! KVStore.get i with
        | None -> return newResponse (sprintf "No contact with id %s" i) "404"
        | Some json -> 
            do! KVStore.delete i 
            return newResponse json"200"
    }

and private noHandler req  =
    newResponse "Invalid contact request" "400" |> wrap
